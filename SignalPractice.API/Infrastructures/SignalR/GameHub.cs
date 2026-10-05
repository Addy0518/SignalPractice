using System.Collections.Concurrent;
using System.Numerics;

namespace Lab.Accounting.API.Infrastructures.SignalR
{
    /// <summary>
    /// 加入遊戲房間的 SignalR 群組 , 玩家進入等待室或遊戲頁面時呼叫
    ///
    /// =========================================================================================================
    /// 後端一整套遊戲流程
    ///
    /// 1. StartGame ( 房主開始遊戲 )
    ///    => 隨機決定畫家是誰
    ///    => 隨機抽三個題目給畫家
    ///    => 廣播告訴房間裡所有人誰正在畫
    ///    => 廣播告訴畫家可以選的題目
    ///
    /// 2-A. ChooseWord ( 畫家選擇了題目 )             2-B. StartChooseTimer ( 畫家在時間內沒選擇題目 )
    ///    => 畫家選擇題目                                  => 開始這回合的選題目計時
    ///    => 標記並套用題目                                => 時間到畫家沒選題目就自動選一個
    ///                                                   => 標記並套用題目
    ///
    /// 3. ApplyChosenWord ( 套用選擇的題目 , 不管 2-A 還是 2-B 都會到這裡 )
    ///    => 設定題目跟畫家
    ///    => 廣播告訴房間裡所有人遊戲開始
    ///    => 廣播告訴畫家題目是什麼
    ///
    /// 4-A. SendGuess ( 玩家送出猜測 )                                      4-B. StartRoundTimer ( 畫家選擇題目後開始倒數計時 )
    ///    => If ( 猜對 ) : 計算剩餘時間並加分 , 廣播告訴房間裡所有人誰猜對             =>  開始這回合的畫圖計時 , 沒有任何人猜對就結束
    ///    => 檢查是否所有人都猜對了 , 如果是就提早結束這輪
    ///    => If ( 猜錯 ) : 廣播告訴房間裡所有人猜題者猜得什麼
    ///
    /// 5. EndRound ( 結束這輪 , 不管 4-A 還是 4-B 都會到這裡 )
    ///    => 廣播告訴房間裡所有人分數報告
    ///    => 判斷是否還有下一輪 , 有就繼續沒有就結束遊戲
    ///
    /// 6. JoinRoom ( 玩家斷線或重連時 )
    ///    => 依照當下是 選題目中 或 畫畫中 來補推送對應的事件給玩家
    ///
    /// 7-A. LeaveRoom ( 玩家主動退出房間 )                                       7-B. OnDisconnectedAsync ( 玩家斷線 , 關閉網頁等等 ... )
    ///      => 房間內玩家不足 2 人 , 直接結束遊戲                                       => 跟 LeaveRoom 一樣的處理
    ///      => 如果退出的是畫家或是選題目的人 , 就跳轉到下一位玩家並根據對應情況處理
    ///
    /// =========================================================================================================
    /// </summary>
    public class GameHub(
        SignalPracticeContext context,
        ILogger<GameHub> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<GameHub> hubContext
    ) : Hub
    {
        private int CurrentUserId => int.Parse(Context.User?.FindFirst("UserId")?.Value ?? "0");

        /// <summary>
        /// 遊戲題目字典 , 存取目前遊戲中用過的所有題目 , 防止接下來重複出題
        /// HashSet 跟 List 差不多都是存集合 , 但 HashSet 不允許重複 , 並且搜索速度快
        /// </summary>
        private static readonly ConcurrentDictionary<int, HashSet<int>> _roomWords = new();

        /// <summary>
        /// 一樣 , 這個則是用來暫存讓玩家選擇的題目
        /// 因為會有好幾個題目 , 所以用集合 ( List )
        /// </summary>
        private static readonly ConcurrentDictionary<int, List<int>> _canChooseWords = new();

        /// <summary>
        /// 每個房間目前這輪計時器的取消權杖
        /// 用來在提早結束（猜對）時取消還沒到期的背景計時器，避免它晚一步醒來時拿舊資料誤觸發下一輪的 EndRound
        /// </summary>
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> _roundTimerCts = new();

        /// <summary>
        /// 一樣是取消權杖 , 不過是選擇題目的
        /// </summary>
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> _chooseTimerCts = new();

        /// <summary>
        /// 一樣是取消權杖 , 不過是斷線重連的
        /// </summary>
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> _disconnTimerCts = new();

        /// <summary>
        /// 斷開連線
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // 斷線重連後先啟動計時器 , 等待 5 秒 , 如果玩家有回來就 return , 沒回來才真的斷線 , 以此確保比如像 F5 刷新網頁時不會觸發斷線邏輯
            _ = StartDisconnectTimer(CurrentUserId, 5);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 開始連線
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 加入用戶專屬群組 , 用來單獨推送只有特定玩家該看到的訊息
        /// </summary>
        public async Task AddToUserGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        /// <summary>
        /// 加入遊戲房間的 SignalR 群組
        /// </summary>
        public async Task JoinRoom(int roomId)
        {
            int userId = CurrentUserId;

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null || room.RoomStatus == RoomStatusEnum.已關閉)
                return;

            var player = await context.RoomPlayers.FirstOrDefaultAsync(r => r.RoomId == roomId && r.PlayerId == userId);

            if (player == null)
                return;

            if (_disconnTimerCts.TryRemove(userId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }

            // Groups 是將這個連線加入指定群組 , 讓在群組裡的人都能收到訊息
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());

            player.IsOnline = 1;
            await context.SaveChangesAsync();

            var playerInfo = new
            {
                player.PlayerId,
                player.PlayerName,
                player.Score,
                player.DrawOrder,
                player.IsReady,
            };

            // 廣播給房間裡所有人：有人加入了
            await Clients.Group(roomId.ToString()).SendAsync("PlayerJoined", playerInfo);

            // 如果這個人是斷線後重新加入 , 並且現在正在遊戲中的話
            // 根據狀況 ( 正在選題目 or 正在畫畫 ) 補推送對應的事件
            if (room.RoomStatus == RoomStatusEnum.遊戲中)
            {
                if (_canChooseWords.TryGetValue(roomId, out var stillChoosing) && stillChoosing.Count() > 0)
                {
                    await Clients.Caller.SendAsync(
                        "DrawerChoosing",
                        new
                        {
                            DrawerId = room.CurrentDrawerId,
                            DrawerName = room
                                .RoomPlayers.FirstOrDefault(p => p.PlayerId == room.CurrentDrawerId)
                                ?.PlayerName,
                            CurrentRound = room.CurrentRound,
                            TotalRound = room.TotalRound,
                        }
                    );

                    if (room.CurrentDrawerId == userId)
                    {
                        var words = stillChoosing
                            .Select(id => context.WorkBanks.Find(id))
                            .Where(w => w != null)
                            .Select(w => new { w!.WorkBankId, w.Word });

                        await Clients.Caller.SendAsync("YourChoosingTopic", words);
                    }
                }
                else
                {
                    // 用 Clients.Caller 而不是 Clients.Group，因為題目只要推給自己就好
                    // 用 Group 推送給所有人的話 , 怕把別人的畫布也重製狀態清空
                    await Clients.Caller.SendAsync(
                        "GameStarted",
                        new
                        {
                            DrawerId = room.CurrentDrawerId,
                            DrawerName = room
                                .RoomPlayers.FirstOrDefault(p => p.PlayerId == room.CurrentDrawerId)
                                ?.PlayerName,
                            CurrentRound = room.CurrentRound,
                            TotalRound = room.TotalRound,
                            RoundSeconds = room.RoundSeconds,
                            RoundEndTime = room.RoundEndTime,
                        }
                    );

                    // 看他是否是畫畫的人 , 是的話就推送題目給他
                    if (room.CurrentDrawerId == userId)
                    {
                        await Clients.Caller.SendAsync("YourTopic", new { Word = room.CurrentTopic });
                    }
                }
            }
        }

        /// <summary>
        /// 離開遊戲房間的 SignalR 群組
        /// </summary>
        public async Task LeaveRoom(int roomId)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            var player = await context.RoomPlayers.FirstOrDefaultAsync(p => p.RoomId == roomId && p.PlayerId == userId);

            if (room == null || player == null)
                return;

            // 從群組移出
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

            // 再呼叫處理玩家離開遊戲的私有方法
            await HandlePlayerLeaving(roomId, userId, room, player, context);
        }

        /// <summary>
        /// 房主開始遊戲
        /// </summary>
        public async Task StartGame(int roomId)
        {
            try
            {
                var userId = CurrentUserId;

                // 每次開新遊戲，重置這個房間的已出題記錄
                _roomWords[roomId] = new HashSet<int>();

                var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null)
                    return;

                // 只有房主才能開始
                if (room.RoomOwnerId != userId)
                {
                    await Clients.Caller.SendAsync("Error", "只有房主可以結束遊戲 !");
                    return;
                }

                // 找出所有在線的玩家 , 並且至少要有兩個人才能開始遊戲
                var onlinePlayers = room.RoomPlayers.Where(p => p.IsOnline == 1).ToList();
                if (onlinePlayers.Count < 2)
                {
                    await Clients.Caller.SendAsync("Error", $"人數不足 , 至少要有兩個人才能開始遊戲 !");
                    return;
                }

                // 先把上一場的分數清掉 , 目前沒有歷史紀錄查詢 , 暫時用這種方法
                foreach (var p in room.RoomPlayers)
                {
                    p.Score = 0;
                }

                // Random.Shared 是 .Net 內建共用實例 , 用來產生隨機變數的 , 比 new Random() 更好用
                // 把在線玩家隨機排序，並且指定畫畫順序
                var AllPlayers = onlinePlayers.OrderBy(_ => Random.Shared.Next()).ToList();
                for (int i = 0; i < AllPlayers.Count(); i++)
                {
                    AllPlayers[i].DrawOrder = i + 1;
                }

                // 找到第一個畫畫的人
                var firstDrawer = AllPlayers.First();

                // 更新房間狀態 ( 先不設定題目因為玩家還沒選 )
                room.RoomStatus = RoomStatusEnum.遊戲中;
                room.CurrentRound = 1;
                room.CurrentDrawerId = firstDrawer.PlayerId;

                await context.SaveChangesAsync();

                // 拿到 3 個能選擇的題目
                var randomWord = await GetWordsNotChooseYet(roomId, context);

                // 暫存進字典
                _canChooseWords[roomId] = randomWord.Select(w => w.WorkBankId).ToList();

                // 廣播給所有人 => 畫家正在選題目
                await Clients
                    .Group(roomId.ToString())
                    .SendAsync(
                        "DrawerChoosing",
                        new
                        {
                            DrawerId = firstDrawer.PlayerId,
                            DrawerName = firstDrawer.PlayerName,
                            CurrentRound = room.CurrentRound,
                            TotalRound = room.TotalRound,
                        }
                    );

                // 單獨推題目給畫畫的人
                await Clients
                    .Group($"user_{firstDrawer.PlayerId}")
                    .SendAsync("YourChoosingTopic", randomWord.Select(w => new { w.WorkBankId, w.Word }));

                // 啟動選題目的計時器
                // 因為這裡要讓他在背景執行 ( 倒數計時 ) , 所以從這裡開始就要新建立 context 了
                _ = StartChooseTimer(roomId, firstDrawer.PlayerId, 10);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        /// <summary>
        /// 房主結束遊戲
        /// </summary>
        public async Task EndGame(int roomId)
        {
            try
            {
                var userId = CurrentUserId;

                var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null || room.RoomStatus != RoomStatusEnum.遊戲中)
                {
                    logger.LogWarning("EndGame 提早結束：房間不存在或狀態不是遊戲中");
                    return;
                }

                // 只有房主才能開始
                if (room.RoomOwnerId != userId)
                {
                    logger.LogWarning(
                        "EndGame 提早結束：呼叫者不是房主 (userId={UserId}, roomOwnerId={RoomOwnerId})",
                        userId,
                        room.RoomOwnerId
                    );
                    await Clients.Caller.SendAsync("Error", "只有房主可以結束遊戲！");
                    return;
                }

                // 結束遊戲
                await FinishGame(room, context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EndGame  發生例外 roomId={roomId}");
            }
        }

        /// <summary>
        /// 進入下一輪
        /// </summary>
        /// <param name="dbContext"> 呼叫端傳入的 DbContext。呼叫鏈的最源頭是 StartRoundTime</param>
        public async Task NextRound(int roomId, SignalPracticeContext dbContext)
        {
            try
            {
                var room = await dbContext
                    .Rooms.Include(r => r.RoomPlayers)
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null)
                    return;

                var currentDrawer = room.RoomPlayers.FirstOrDefault(p => p.PlayerId == room.CurrentDrawerId);

                if (currentDrawer == null)
                    return;

                // 換下一個畫畫的人
                var nextDrawer = GetNextDrawer(room);

                if (nextDrawer == null)
                {
                    // 沒人在線上了，直接結束遊戲
                    await FinishGame(room, dbContext);
                    return;
                }

                // 更新房間狀態
                room.CurrentRound = room.CurrentRound + 1;
                room.CurrentDrawerId = nextDrawer.PlayerId;

                await dbContext.SaveChangesAsync();

                // 接下來就跟 StartGame 一樣
                var randomWord = await GetWordsNotChooseYet(roomId, dbContext);

                _canChooseWords[roomId] = randomWord.Select(w => w.WorkBankId).ToList();

                await hubContext
                    .Clients.Group(roomId.ToString())
                    .SendAsync(
                        "DrawerChoosing",
                        new
                        {
                            DrawerId = nextDrawer.PlayerId,
                            DrawerName = nextDrawer.PlayerName,
                            CurrentRound = room.CurrentRound,
                            TotalRound = room.TotalRound,
                        }
                    );

                await hubContext
                    .Clients.Group($"user_{nextDrawer.PlayerId}")
                    .SendAsync("YourChoosingTopic", randomWord.Select(w => new { w.WorkBankId, w.Word }));

                _ = StartChooseTimer(roomId, nextDrawer.PlayerId, 10);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ NextRound 發生例外 roomId={roomId}");
            }
        }

        /// <summary>
        /// 畫家選擇題目
        /// </summary>
        public async Task ChooseWord(int roomId, int workBankId)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return;

            // 只有畫畫的人才能選題目
            if (room.CurrentDrawerId != userId)
            {
                await Clients.Caller.SendAsync("Error", "現在不是你選題目的時候！");
                return;
            }

            // 確認這個題目是暫存的題目之一
            if (!_canChooseWords.TryGetValue(roomId, out var availableWords) || !availableWords.Contains(workBankId))
            {
                await Clients.Caller.SendAsync("Error", "非法的選字！");
                return;
            }

            // 從資料庫拿到這個題目
            var word = await context.WorkBanks.FindAsync(workBankId);
            if (word == null)
                return;

            // 標記用過的題目
            MarkWordUsed(roomId, workBankId);

            // 把剛暫存的題目清掉，避免下一輪還有舊題目
            _canChooseWords.TryRemove(roomId, out _);

            // 取消選題目的計時器，避免它在背景醒來時誤觸發下一輪的 EndRound
            if (_chooseTimerCts.TryGetValue(roomId, out var chooseCts))
            {
                chooseCts.Cancel();
            }

            // 最後套用選好的題目並開始遊戲
            await ApplyChosenWord(roomId, word, room, context);
        }

        /// <summary>
        /// 玩家送出猜測
        /// </summary>
        public async Task SendGuess(int roomId, string guess)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return;

            // 畫畫的人不能猜
            if (userId == room.CurrentDrawerId)
                return;

            var player = room.RoomPlayers.FirstOrDefault(p => p.PlayerId == userId);
            if (player == null)
                return;

            bool isCorrect = guess.Trim() == room.CurrentTopic?.Trim();

            var message = new GuessMessage
            {
                RoomId = roomId,
                PlayerId = userId,
                PlayerName = player.PlayerName,
                // 如果猜對了就顯示 "***" ，否則顯示玩家猜的內容
                Content = isCorrect ? "***" : guess,
                IsCorrect = isCorrect,
                CreateTime = DateTime.UtcNow,
            };

            await context.GuessMessages.AddAsync(message);

            if (isCorrect)
            {
                const int minScore = 10;
                const int maxScore = 100;

                // 計算玩家這回合答對時的剩餘時間
                // Math.Max 取最大值 , 避免時間計算完是負數 , 造成分數計算錯誤 ( 如果是負數就比 0 小 , 就取最大值 0 )
                var remainingSeconds = Math.Max(0, (room.RoundEndTime.Value - DateTime.UtcNow).TotalSeconds);

                // ( 這輪的剩餘時間 / 這輪的總時間 ) = 這個玩家答對的速度比例
                // 這個比例越大就代表玩家答對的越快 , 分數就會越高 ( 假設他在剩 45 秒時答出來 , 就是 45/60 = 0.75 ) , 以此類推
                // Math.Min 一樣是保險機制 , 確保不會超過 1 ( 1 就最快了 )
                var remainingRatio = Math.Min(1, remainingSeconds / room.RoundSeconds);

                // 計算分數 = 最低分 + ( 最高分 - 最低分 ) * 剩餘時間比例
                // 假設他一開始就答對 , remainingRatio = 1 , score 就等於 10 + (100 - 10) * 1 = 100 ( 滿分 )
                int score = (int)Math.Round(minScore + (maxScore - minScore) * remainingRatio);

                player.Score += score;

                await context.SaveChangesAsync();

                // 廣播給所有人猜中的訊息
                await Clients
                    .Group(roomId.ToString())
                    .SendAsync(
                        "GuessCorrect",
                        new
                        {
                            PlayerId = player.PlayerId,
                            PlayerName = player.PlayerName,
                            Score = score,
                            TotalScore = player.Score,
                        }
                    );

                var onlinePlayers = room
                    .RoomPlayers.Where(p => p.IsOnline == 1 && p.PlayerId != room.CurrentDrawerId)
                    .ToList();

                // 計算這輪開始的時間，因為猜對的人可能在這輪結束前才猜對，所以要用 RoundEndTime 減去 RoundSeconds 才能得到這輪開始的時間
                var roundStartTime = room.RoundEndTime.Value.AddSeconds(-room.RoundSeconds);

                // 查看所有猜對的人
                var correctPlayers = await context
                    .GuessMessages.Where(m =>
                        m.RoomId == roomId
                        && m.IsCorrect
                        && m.PlayerId != room.CurrentDrawerId
                        && m.CreateTime >= roundStartTime
                    )
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .CountAsync();

                // 如果所有人都猜對了就提早結束這回合
                if (correctPlayers >= onlinePlayers.Count)
                {
                    if (_roundTimerCts.TryGetValue(roomId, out var cts))
                    {
                        cts.Cancel();
                    }
                    await EndRound(roomId, context);
                }
            }
            else
            {
                await context.SaveChangesAsync();

                // 廣播給所有人訊息
                await Clients
                    .Group(roomId.ToString())
                    .SendAsync(
                        "GuessInCorrect",
                        new
                        {
                            PlayerId = player.PlayerId,
                            PlayerName = player.PlayerName,
                            Content = guess,
                            IsCorrect = false,
                        }
                    );
            }
        }

        /// <summary>
        /// 傳送畫線資料給房間裡所有人
        /// </summary>
        /// <param name="roomId">房間 ID</param>
        /// <param name="points">座標陣列 [x1,y1,x2,y2,...]</param>
        /// <param name="color">線條顏色</param>
        /// <param name="strokeWidth">線條粗細</param>
        public async Task SendDraw(int roomId, float[] points, string color, int strokeWidth)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return;

            // 只有畫畫的人才能傳送畫線資料
            if (userId != room.CurrentDrawerId)
                return;

            // OthersInGroup 廣播給所有人除了自己
            await Clients
                .OthersInGroup(roomId.ToString())
                .SendAsync(
                    "ReceiveDraw",
                    new
                    {
                        Points = points,
                        Color = color,
                        StrokeWidth = strokeWidth,
                    }
                );
        }

        /// <summary>
        /// 清除畫布
        /// </summary>
        public async Task ClearDraw(int roomId)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
                return;

            // 只有畫畫的人才能清除畫布
            if (userId != room.CurrentDrawerId)
                return;

            // 廣播給所有人要清除畫布了
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("ClearDraw");
        }

        /// <summary>
        /// 每輪的計時器，時間到自動結束這輪
        ///
        /// 補充概念 :
        /// SignalR 的 Hub 是「每次方法呼叫」建立一個新的實例 , 假設某個方法 ( 比如 StartGame ) 被呼叫完後就會丟掉 , 包括建構子注入的 context 也一樣
        /// 這也是為何 StartRoundTimer 這個方法要再多實例出一個 context , 因為她有延遲計算的邏輯 ( Task.Delay ) , 他是在倒數完 60 秒之後才執行 , 所以當執行到這時原本的 context 早就被丟掉了
        ///
        /// 簡單區分的話就是呼叫當下就執行 , 就用一般 context 就好
        /// 不是在呼叫當下執行的就要在新創一個 context
        /// </summary>
        private async Task StartRoundTimer(int roomId, int seconds)
        {
            // 啟動新計時器前，先把同房間上一個還沒到期的舊計時器取消掉
            if (_roundTimerCts.TryGetValue(roomId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            // CancellationTokenSource 是這個計時器的取消按鈕
            var cts = new CancellationTokenSource();

            // 把按鈕登記在字典裡 , 要取消計時就回來用 key 去字典裡找這個對應的按鈕
            _roundTimerCts[roomId] = cts;

            try
            {
                // 等待這輪的秒數
                // 傳 Token 是能夠
                await Task.Delay(TimeSpan.FromSeconds(seconds), cts.Token);
            }
            catch (TaskCanceledException)
            {
                // 被提早取消，代表這輪已經結束了，不用再做事
                return;
            }

            // using 確保這個新開的 scope（連同裡面的 DbContext）在用完後會被正確釋放
            // 不會造成 DbContext 一直堆積、記憶體洩漏
            using var scope = scopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<SignalPracticeContext>();

            // 時間到，執行 EndRound ( 自訂的方法 )
            await EndRound(roomId, scopedContext);
        }

        /// <summary>
        /// 選題目的計時器，時間到畫家還沒選的話自動幫他選一個
        /// </summary>
        private async Task StartChooseTimer(int roomId, int drawerId, int seconds)
        {
            // 啟動新計時器前，先把同房間上一個還沒到期的舊計時器取消掉
            if (_chooseTimerCts.TryGetValue(roomId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _chooseTimerCts[roomId] = cts;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), cts.Token);
            }
            catch (TaskCanceledException)
            {
                // 被提早取消，代表這輪已經結束了，不用再做事
                return;
            }

            // 題目已經被清掉，代表畫家在時間內已經自己選了，不用自動選
            if (!_canChooseWords.TryGetValue(roomId, out var candidates) || candidates.Count == 0)
                return;

            // using 確保這個新開的 scope（連同裡面的 DbContext）在用完後會被正確釋放
            // 不會造成 DbContext 一直堆積、記憶體洩漏
            using var scope = scopeFactory.CreateScope();
            var scopedContext = scope.ServiceProvider.GetRequiredService<SignalPracticeContext>();

            var room = await scopedContext
                .Rooms.Include(r => r.RoomPlayers)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null || room.RoomStatus != RoomStatusEnum.遊戲中)
                return;

            if (room.CurrentDrawerId != drawerId)
                return;

            // 因為他還沒選題目，所以就隨機幫他選一個
            var autoChooseWordId = candidates[Random.Shared.Next(candidates.Count())];
            var word = await scopedContext.WorkBanks.FindAsync(autoChooseWordId);
            if (word == null)
                return;

            // 標記用過的題目
            MarkWordUsed(roomId, word.WorkBankId);

            // 把剛暫存的題目清掉，避免下一輪還有舊題目
            _canChooseWords.TryRemove(roomId, out _);

            await ApplyChosenWord(roomId, word, room, scopedContext);
        }

        /// <summary>
        /// 斷線重連的計時器
        /// </summary>
        private async Task StartDisconnectTimer(int userId, int seconds)
        {
            // 啟動新計時器前，先把同房間上一個還沒到期的舊計時器取消掉
            if (_disconnTimerCts.TryGetValue(userId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            var cts = new CancellationTokenSource();
            _disconnTimerCts[userId] = cts;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), cts.Token);
            }
            catch (TaskCanceledException)
            {
                // 被提早取消，代表這輪已經結束了，不用再做事
                return;
            }

            // 倒數跑完後如果沒提早取消 , 就把字典裡的按鈕刪掉
            _disconnTimerCts.TryRemove(userId, out _);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SignalPracticeContext>();

                var player = await db.RoomPlayers.FirstOrDefaultAsync(c => c.PlayerId == userId && c.IsOnline == 1);

                if (player != null)
                {
                    var room = await db
                        .Rooms.Include(r => r.RoomPlayers)
                        .FirstOrDefaultAsync(r => r.RoomId == player.RoomId);
                    if (room != null)
                    {
                        await HandlePlayerLeaving(room.RoomId, userId, room, player, db);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "斷線寬限期處理失敗 userId={UserId}", userId);
            }
        }

        /// <summary>
        /// 套用選好的題目並開始遊戲
        /// </summary>
        private async Task ApplyChosenWord(int roomId, WorkBank word, Room room, SignalPracticeContext dbContext)
        {
            var drawer = room.RoomPlayers.FirstOrDefault(p => p.PlayerId == room.CurrentDrawerId);
            if (drawer == null)
                return;

            // 設定題目跟畫家
            // 這時候才開始設置 RoundTime , 因為畫家選題目時不會倒數 , 只有選完題目才開始倒數
            room.CurrentDrawerId = drawer.PlayerId;
            room.CurrentTopic = word.Word;
            room.RoundEndTime = DateTime.UtcNow.AddSeconds(room.RoundSeconds);
            await dbContext.SaveChangesAsync();

            // 廣播給所有人 => 遊戲開始
            await hubContext
                .Clients.Group(roomId.ToString())
                .SendAsync(
                    "GameStarted",
                    new
                    {
                        DrawerId = drawer.PlayerId,
                        DrawerName = drawer.PlayerName,
                        CurrentRound = room.CurrentRound,
                        TotalRound = room.TotalRound,
                        RoundSeconds = room.RoundSeconds,
                        RoundEndTime = room.RoundEndTime,
                    }
                );

            // 廣播推送題目給畫畫的人
            await hubContext.Clients.Group($"user_{drawer.PlayerId}").SendAsync("YourTopic", new { Word = word.Word });

            // 啟動這輪的畫圖倒數計時
            _ = StartRoundTimer(roomId, room.RoundSeconds);
        }

        /// <summary>
        /// 結束這一輪
        /// </summary>
        private async Task EndRound(int roomId, SignalPracticeContext dbContext)
        {
            try
            {
                var room = await dbContext
                    .Rooms.Include(r => r.RoomPlayers)
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null || room.RoomStatus != RoomStatusEnum.遊戲中)
                    return;

                // 公布這輪答案和分數給所有人
                await hubContext
                    .Clients.Group(roomId.ToString())
                    .SendAsync(
                        "RoundEnd",
                        new
                        {
                            Word = room.CurrentTopic,
                            Scores = room.RoomPlayers.Select(p => new
                            {
                                p.PlayerId,
                                p.PlayerName,
                                p.Score,
                            }),
                        }
                    );

                // 因為前端每輪會有五秒的分數報告時間 , 所以後端這裡也要延遲五秒
                await Task.Delay(TimeSpan.FromSeconds(5));

                // 這裡因為 Delay 了五秒所以要再拿一次 room 的資料 , 確保資料為最新的
                room = await dbContext.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null || room.RoomStatus != RoomStatusEnum.遊戲中)
                    return;

                // 判斷是否還有下一輪
                if (room.CurrentRound >= room.TotalRound)
                {
                    // 遊戲結束
                    await FinishGame(room, dbContext);
                }
                else
                {
                    // 進入下一輪
                    await NextRound(roomId, dbContext);
                }
            }
            catch (Exception ex)
            {
                // ✅ 關鍵：把背景工作裡的例外印出來，不然會被靜默吞掉
                logger.LogError(ex, $"❌ EndRound 發生例外 roomId={roomId}");
            }
        }

        /// <summary>
        /// 抽取 N 個還未用過的題目
        /// </summary>
        /// <param name="count">能選擇的題目數量</param>
        private async Task<List<WorkBank>> GetWordsNotChooseYet(
            int roomId,
            SignalPracticeContext dbContext,
            int count = 3
        )
        {
            // 先從 _roomWords 這個字典裡找出這個房間已經用過的題目 , 沒用過題目的話就創建新集合 ( new HashSet )
            var useWordIds = _roomWords.TryGetValue(roomId, out var ids) ? ids : new HashSet<int>();

            // 拿出資料庫裡還沒用過的題目集合
            var canUseWords = await dbContext.WorkBanks.Where(w => !useWordIds.Contains(w.WorkBankId)).ToListAsync();

            // 如果資料庫題目都用完了 , 就清空已用過題目集合 , 重新拿出資料庫裡所有題目
            if (canUseWords.Count == 0)
            {
                useWordIds.Clear();
                canUseWords = await dbContext.WorkBanks.ToListAsync();
            }

            // 打亂順序後隨即抽取集合裡的第 N 個題目 ( count 決定抽取第幾個題目 )
            return canUseWords.OrderBy(_ => Random.Shared.Next()).Take(count).ToList();
        }

        /// <summary>
        /// 標記已用過的題目
        /// </summary>
        private void MarkWordUsed(int roomId, int wordBankId)
        {
            // 如果 _roomWords 這個字典裡沒有集合的話 ( 第一次 ) , 就回傳 false , 加上 ! 變 true
            if (!_roomWords.TryGetValue(roomId, out var useWordIds))
            {
                // 第一次 , 那就創建一個新的集合來放題目 ID
                useWordIds = new HashSet<int>();

                // 並以 roomId 為 key , 新增到 _roomWords 字典裡 , 這樣下一次就能找到這個房間已用過的題目集合
                _roomWords[roomId] = useWordIds;
            }

            // 不管是不是第一次都要把題目加入已用過題目集合
            useWordIds.Add(wordBankId);
        }

        /// <summary>
        /// 切換到下一輪的畫家
        /// </summary>
        private RoomPlayer? GetNextDrawer(Room room, int? excludePlayerId = null)
        {
            // 拿到還在線上並且不是被排除的玩家，並依照 DrawOrder 排序
            var onlinePlayers = room
                .RoomPlayers.Where(p => p.IsOnline == 1 && p.PlayerId != excludePlayerId)
                .OrderBy(p => p.DrawOrder)
                .ToList();

            if (onlinePlayers.Count() == 0)
                return null;

            // 再從這份名單拿到下一個畫家
            var currentIndex = onlinePlayers.FindIndex(p => p.PlayerId == room.CurrentDrawerId);

            // 如果找不到 ( currentIndex == -1 ) , 就從第一個開始
            // 找到的話 ,  就 + 1 往下一位 , % onlinePlayers.Count 確保不會超過名單長度 , 這樣就能循環回到第一位
            var nextIndex = currentIndex == -1 ? 0 : (currentIndex + 1) % onlinePlayers.Count;
            return onlinePlayers[nextIndex];
        }

        /// <summary>
        /// 處理玩家離開房間
        /// </summary>
        private async Task HandlePlayerLeaving(
            int roomId,
            int userId,
            Room room,
            RoomPlayer player,
            SignalPracticeContext dbcontext
        )
        {
            player.IsOnline = 0;
            await dbcontext.SaveChangesAsync();

            // 廣播給房間裡所有人：有人離開了
            await hubContext.Clients.Group(roomId.ToString()).SendAsync("PlayerLeft", userId);

            // 如果離開的人是房主，則將房主轉移給下一個在線的玩家
            if (userId == room.RoomOwnerId)
            {
                var newOwner = room
                    .RoomPlayers.Where(p => p.IsOnline == 1 && p.PlayerId != userId)
                    .OrderBy(p => p.JoinTime)
                    .FirstOrDefault();

                if (newOwner == null)
                {
                    room.RoomStatus = RoomStatusEnum.已關閉;
                    await dbcontext.SaveChangesAsync();
                    return;
                }
                room.RoomOwnerId = newOwner.PlayerId;
                await dbcontext.SaveChangesAsync();

                await hubContext
                    .Clients.Group(roomId.ToString())
                    .SendAsync("HostChanged", new { OwnerId = newOwner.PlayerId, OwnerName = newOwner.PlayerName });
            }

            if (room.RoomStatus != RoomStatusEnum.遊戲中)
                return;

            // 檢查那個人離開後，房間裡還有多少人在線上
            var stillOnline = room.RoomPlayers.Count(p => p.IsOnline == 1);

            // 人數不足以繼續遊戲，直接結束
            if (stillOnline < 2)
            {
                await FinishGame(room, dbcontext);
                return;
            }

            // 如果不是 「 正在選字或畫圖的人 」 離開的話 , 就不用繼續往下走
            if (room.CurrentDrawerId != userId)
                return;

            // 判斷是還在選題目階段，還是已經在畫圖階段
            if (_canChooseWords.TryGetValue(roomId, out var stillChoosing) && stillChoosing.Count > 0)
            {
                // 用 _chooseTimerCts 這個字典判斷有沒有題目在 , 有的話就代表還在選題目階段
                if (_chooseTimerCts.TryGetValue(roomId, out var chooseCts))
                    chooseCts.Cancel();
                _canChooseWords.TryRemove(roomId, out _);

                var nextDrawer = GetNextDrawer(room, excludePlayerId: userId);
                if (nextDrawer == null)
                    return;

                room.CurrentDrawerId = nextDrawer.PlayerId;
                await dbcontext.SaveChangesAsync();

                var randomWord = await GetWordsNotChooseYet(roomId, dbcontext);
                _canChooseWords[roomId] = randomWord.Select(w => w.WorkBankId).ToList();

                await hubContext
                    .Clients.Group(roomId.ToString())
                    .SendAsync(
                        "DrawerChoosing",
                        new
                        {
                            DrawerId = nextDrawer.PlayerId,
                            DrawerName = nextDrawer.PlayerName,
                            CurrentRound = room.CurrentRound,
                            TotalRound = room.TotalRound,
                        }
                    );
                await hubContext
                    .Clients.Group($"user_{nextDrawer.PlayerId}")
                    .SendAsync("YourChoosingTopic", randomWord.Select(w => new { w.WorkBankId, w.Word }));

                _ = StartChooseTimer(roomId, nextDrawer.PlayerId, 10);
            }
            else
            {
                // 如果這個房間有一個還在跑的畫圖倒數計時器,把它取消掉
                if (_roundTimerCts.TryGetValue(roomId, out var roundCts))
                    roundCts.Cancel();

                // 因為 EndRound 裡會有一個五秒的延遲時間 , 所以這裡直接改成背景執行 , 就不會有退出後前端畫面還卡在原地的狀況
                _ = Task.Run(async () =>
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedContext = scope.ServiceProvider.GetRequiredService<SignalPracticeContext>();
                    await EndRound(roomId, scopedContext);
                });
            }
        }

        /// <summary>
        /// 遊戲結束存入分數跟排行
        /// </summary>
        private async Task FinishGame(Room room, SignalPracticeContext dbContext)
        {
            if (room.RoomStatus == RoomStatusEnum.已結束)
                return;

            // 遊戲結束
            room.RoomStatus = RoomStatusEnum.已結束;

            var ranked = room.RoomPlayers.OrderByDescending(p => p.Score).ToList();

            var record = new SignalPractice.API.Models.GameRecord
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                TotalRound = room.TotalRound,
                RoundSeconds = room.RoundSeconds,
                CreateTime = DateTime.UtcNow,
                GameRecordPlayers = ranked
                    .Select(
                        (p, i) =>
                            new GameRecordPlayer
                            {
                                PlayerId = p.PlayerId,
                                PlayerName = p.PlayerName,
                                Score = p.Score,
                                // index 是從 0 開始的，所以要 +1 才是正確的名次
                                Rank = i + 1,
                            }
                    )
                    .ToList(),
            };

            dbContext.GameRecords.Add(record);

            await dbContext.SaveChangesAsync();

            await hubContext
                .Clients.Group(room.RoomId.ToString())
                .SendAsync(
                    "GameEnd",
                    new
                    {
                        Rankings = room
                            .RoomPlayers.OrderByDescending(p => p.Score)
                            .Select(
                                (p, index) =>
                                    new
                                    {
                                        Rank = index + 1,
                                        p.PlayerId,
                                        p.PlayerName,
                                        p.Score,
                                    }
                            ),
                    }
                );
        }
    }
}
