namespace Lab.Accounting.API.Infrastructures.SignalR
{
    /// <summary>
    /// 加入遊戲房間的 SignalR 群組 , 玩家進入等待室或遊戲頁面時呼叫
    /// </summary>
    public class GameHub(SignalPracticeContext context) : Hub
    {
        private int CurrentUserId => int.Parse(Context.User?.FindFirst("UserId")?.Value ?? "0");

        /// <summary>
        /// 遊戲題目字典 , 存取目前遊戲中用過的所有題目 , 防止接下來重複出題
        /// HashSet 跟 List 差不多都是存集合 , 但 HashSet 不允許重複 , 並且搜索速度快
        /// </summary>
        private static readonly Dictionary<int, HashSet<int>> _roomWords = new();

        /// <summary>
        /// 使用者斷線時清除 , base.OnDisconnectedAsync 就是斷線時觸發
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// 加入遊戲房間的 SignalR 群組
        /// </summary>
        public async Task JoinRoom(int roomId)
        {
            int userId = CurrentUserId;

            var player = await context.RoomPlayers.FirstOrDefaultAsync(r => r.RoomId == roomId && r.PlayerId == userId);

            if (player == null)
                return;
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
        }

        /// <summary>
        /// 離開遊戲房間的 SignalR 群組
        /// </summary>
        public async Task LeaveRoom(int roomId)
        {
            var userId = CurrentUserId;

            var player = await context.RoomPlayers.FirstOrDefaultAsync(p => p.RoomId == roomId && p.PlayerId == userId);

            if (player == null)
                return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());

            player.IsOnline = 0;
            await context.SaveChangesAsync();

            // 廣播給房間裡所有人：有人離開了
            await Clients.Group(roomId.ToString()).SendAsync("PlayerLeft", userId);
        }

        /// <summary>
        /// 房主開始遊戲
        /// </summary>
        public async Task StartGame(int roomId)
        {
            var userId = CurrentUserId;

            _roomWords[roomId] = new HashSet<int>();

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return;

            // 只有房主才能開始
            if (room.RoomOwnerId != userId)
                return;

            // 找出所有在線的玩家 , 並且至少要有兩個人才能開始遊戲
            var onlinePlayers = room.RoomPlayers.Where(p => p.IsOnline == 1).ToList();
            if (onlinePlayers.Count < 2)
                return;

            // Random.Shared 是 .Net 內建共用實例 , 用來產生隨機變數的 , 比 new Random() 更好用
            // 把在線玩家隨機排序，並且指定畫畫順序
            var AllPlayers = onlinePlayers.OrderBy(_ => Random.Shared.Next()).ToList();
            for (int i = 0; i < AllPlayers.Count(); i++)
            {
                AllPlayers[i].DrawOrder = i + 1;
            }

            // 找到第一個畫畫的人
            var firstDrawer = AllPlayers.First();

            // 自訂的隨機指定題目方法
            var randomWord = await GetRoomsWord(roomId);

            // 更新房間狀態
            room.RoomStatus = RoomStatusEnum.遊戲中;
            room.CurrentRound = 1;
            room.CurrentDrawerId = firstDrawer.PlayerId;
            room.CurrentTopic = randomWord?.Word;
            room.RoundEndTime = DateTime.UtcNow.AddSeconds(room.RoundSeconds);

            await context.SaveChangesAsync();

            // 廣播給所有人遊戲開始
            // 不包含題目，題目只推給畫畫的人
            await Clients
                .Group(roomId.ToString())
                .SendAsync(
                    "GameStarted",
                    new
                    {
                        DrawerId = firstDrawer.PlayerId,
                        DrawerName = firstDrawer.PlayerName,
                        CurrentRound = room.CurrentRound,
                        TotalRound = room.TotalRound,
                        RoundSeconds = room.RoundSeconds,
                        RoundEndTime = room.RoundEndTime,
                    }
                );

            // 單獨推題目給畫畫的人
            await Clients.Group($"user_{firstDrawer.PlayerId}").SendAsync("YourTopic", new { Word = randomWord?.Word });

            // 啟動計時器
            _ = StartRoundTimer(roomId, room.RoundSeconds);
        }

        /// <summary>
        /// 進入下一輪
        /// </summary>
        public async Task NextRound(int roomId)
        {
            var userId = CurrentUserId;

            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
                return;

            var currentDrawer = room.RoomPlayers.FirstOrDefault(p => p.PlayerId == room.CurrentDrawerId);

            // 換下一個畫畫的人
            int nextDrawOrder = currentDrawer.DrawOrder + 1;

            var onlinePlayers = room.RoomPlayers.Count(p => p.IsOnline == 1);

            // 如果下一個畫畫的人超過在線玩家數量，則回到第一個畫畫的人
            if (nextDrawOrder > onlinePlayers)
            {
                nextDrawOrder = 1;
            }

            var nextDrawer = room.RoomPlayers.FirstOrDefault(p => p.DrawOrder == nextDrawOrder);

            // 自訂的隨機指定題目方法
            var randomWord = await GetRoomsWord(roomId);

            // 更新房間狀態
            room.CurrentRound = room.CurrentRound + 1;
            room.CurrentDrawerId = nextDrawer.PlayerId;
            room.CurrentTopic = randomWord?.Word;
            room.RoundEndTime = DateTime.UtcNow.AddSeconds(room.RoundSeconds);

            await context.SaveChangesAsync();

            // 廣播給所有人遊戲開始
            // 不包含題目，題目只推給畫畫的人
            await Clients
                .Group(roomId.ToString())
                .SendAsync(
                    "GameStarted",
                    new
                    {
                        DrawerId = nextDrawer.PlayerId,
                        DrawerName = nextDrawer.PlayerName,
                        CurrentRound = room.CurrentRound,
                        TotalRound = room.TotalRound,
                        RoundSeconds = room.RoundSeconds,
                        RoundEndTime = room.RoundEndTime,
                    }
                );

            // 單獨推題目給畫畫的人
            await Clients.Group($"user_{nextDrawer.PlayerId}").SendAsync("YourTopic", new { Word = randomWord?.Word });

            // 啟動計時器
            _ = StartRoundTimer(roomId, room.RoundSeconds);
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
                int score = 10;
                player.Score += score;

                await context.SaveChangesAsync();

                // 廣播給所有人猜中的訊息
                await Clients
                    .Group(roomId.ToString())
                    .SendAsync(
                        "GuessCorrect",
                        new
                        {
                            DrawerId = player.PlayerId,
                            DrawerName = player.PlayerName,
                            Score = score,
                            TotalScore = player.Score,
                        }
                    );

                var onlinePlayers = room
                    .RoomPlayers.Where(p => p.IsOnline == 1 && p.PlayerId != room.CurrentDrawerId)
                    .ToList();

                // 查看所有猜對的人
                var correctPlayers = await context
                    .GuessMessages.Where(m =>
                        m.RoomId == roomId
                        && m.IsCorrect
                        && m.PlayerId != room.CurrentDrawerId
                        && m.CreateTime >= room.RoundEndTime
                    )
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .CountAsync();

                // 如果所有人都猜對了就提早結束這回合
                if (correctPlayers >= onlinePlayers.Count)
                {
                    await EndRound(roomId);
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
                            DrawerId = player.PlayerId,
                            DrawerName = player.PlayerName,
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
        /// </summary>
        private async Task StartRoundTimer(int roomId, int seconds)
        {
            // 等待這輪的秒數
            await Task.Delay(TimeSpan.FromSeconds(seconds));

            // 時間到，執行 EndRound ( 自訂的方法 )
            await EndRound(roomId);
        }

        /// <summary>
        /// 結束這一輪
        /// </summary>
        private async Task EndRound(int roomId)
        {
            var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null || room.RoomStatus != RoomStatusEnum.遊戲中)
                return;

            // 公布這輪答案和分數給所有人
            await Clients
                .Group(roomId.ToString())
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

            // 判斷是否還有下一輪
            if (room.CurrentRound >= room.TotalRound)
            {
                // 遊戲結束
                room.RoomStatus = RoomStatusEnum.已結束;
                await context.SaveChangesAsync();

                await Clients
                    .Group(roomId.ToString())
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
                                            // index 是從 0 開始的，所以要 +1 才是正確的名次
                                            Rank = index + 1,
                                            p.PlayerId,
                                            p.PlayerName,
                                            p.Score,
                                        }
                                ),
                        }
                    );
            }
            else
            {
                // 進入下一輪
                await NextRound(roomId);
            }
        }

        /// <summary>
        /// 抽取一個還未用過的題目
        /// </summary>
        private async Task<WorkBank> GetRoomsWord(int roomId)
        {
            // 先從 _roomWords 這個字典裡找出這個房間已經用過的題目 , 沒用過題目的話就創建新集合 ( new HashSet )
            var useWordIds = _roomWords.TryGetValue(roomId, out var ids) ? ids : new HashSet<int>();

            // 拿出資料庫裡還沒用過的題目集合
            var canUseWords = await context.WorkBanks.Where(w => !useWordIds.Contains(w.WorkBankId)).ToListAsync();

            // 如果資料庫題目都用完了 , 就清空已用過題目集合 , 重新拿出資料庫裡所有題目
            if (canUseWords.Count == 0)
            {
                useWordIds.Clear();
                canUseWords = await context.WorkBanks.ToListAsync();
            }

            // 隨機指定題目
            var randomWord = canUseWords[Random.Shared.Next(canUseWords.Count)];

            // 把這個題目加入已用過題目集合
            useWordIds.Add(randomWord.WorkBankId);
            _roomWords[roomId] = useWordIds;

            return randomWord;
        }
    }
}
