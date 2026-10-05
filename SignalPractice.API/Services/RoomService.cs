namespace SignalPractice.API.Services;

public class RoomService(
    SignalPracticeContext context,
    TokenHelper tokenHelper,
    PasswordSecureHelper passwordSecureHelper
) : IRoomService
{
    /// <summary>
    /// 查看房間資訊
    /// </summary>
    /// <param name="roomCode">房間代碼</param>
    /// <param name="userId">玩家 ID</param>
    /// <returns>房間資訊</returns>
    public async Task<ApiResponse<RoomResponse>> GetRoomInfo(string roomCode, int userId)
    {
        var room = await context.Rooms.Include(r => r.RoomPlayers).FirstOrDefaultAsync(r => r.RoomCode == roomCode);

        if (room == null || room.RoomStatus == RoomStatusEnum.已關閉)
        {
            var errors = new Dictionary<string, string[]> { { "RoomCode", new[] { "找不到房間！" } } };
            return ApiResponseHelper.RequestError<RoomResponse>(errors);
        }

        // 檢查是否在這個房間
        bool isExist = await context.RoomPlayers.AnyAsync(r => r.RoomId == room.RoomId && r.PlayerId == userId);
        if (!isExist)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "UserId", new[] { "你不在這個房間 , 無法查看房間資訊 ！" } },
            };
            return ApiResponseHelper.RequestError<RoomResponse>(errors);
        }

        var response = new RoomResponse
        {
            RoomId = room.RoomId,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            RoomOwnerId = room.RoomOwnerId,
            RoomStatus = room.RoomStatus,
            TotalRound = room.TotalRound,
            CurrentRound = room.CurrentRound,
            CurrentDrawerId = room.CurrentDrawerId,
            RoundSeconds = room.RoundSeconds,
            RoundEndTime = room.RoundEndTime,
            Players = room
                .RoomPlayers.Select(p => new RoomPlayerResponse
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.PlayerName,
                    Score = p.Score,
                    DrawOrder = p.DrawOrder,
                    IsOnline = p.IsOnline,
                    IsReady = p.IsReady,
                })
                .ToList(),
        };

        return ApiResponseHelper.Success(response);
    }

    /// <summary>
    /// 創建房間
    /// </summary>
    /// <param name="request">房間資訊</param>
    /// <param name="userId">創建者 ID</param>
    /// <param name="userName">創建者姓名</param>
    /// <returns>房間代碼</returns>
    public async Task<ApiResponse<string>> CreateRoom(RoomCreateRequest request, int userId, string userName)
    {
        // 檢查房間是否已存在
        bool isExist = await context
            .Rooms.Where(r => r.RoomStatus != RoomStatusEnum.已關閉)
            .AnyAsync(r => r.RoomName == request.RoomName);
        if (isExist)
        {
            var errors = new Dictionary<string, string[]> { { "RoomName", new[] { "無法重複創建房間！" } } };
            return ApiResponseHelper.RequestError<string>(errors);
        }

        string roomCode;

        // 當資料庫有相同代碼的話就繼續生成
        do
        {
            roomCode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6).ToUpper();
        } while (await context.Rooms.AnyAsync(r => r.RoomCode == roomCode));

        var room = new Room
        {
            RoomOwnerId = userId,
            RoomCode = roomCode,
            RoomName = request.RoomName,
            RoomStatus = RoomStatusEnum.等待中,
            TotalRound = request.TotalRound,
            CurrentRound = 0,
            RoundSeconds = request.RoundSeconds,
            CreateTime = DateTime.UtcNow,
        };
        await context.Rooms.AddAsync(room);
        await context.SaveChangesAsync();
        // 創建房間後，將創建者加入房間玩家列表
        var hostPlayer = new RoomPlayer
        {
            RoomId = room.RoomId,
            PlayerId = userId,
            PlayerName = userName,
            Score = 0,
            DrawOrder = 1,
            IsOnline = 1,
            IsReady = 0,
            JoinTime = DateTime.UtcNow,
        };

        await context.RoomPlayers.AddAsync(hostPlayer);
        await context.SaveChangesAsync();

        return ApiResponseHelper.Success(roomCode);
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    /// <param name="roomCode">房間代碼</param>
    /// <param name="userId">玩家 ID</param>
    /// <param name="userName">玩家姓名</param>
    /// <returns>是否成功</returns>
    public async Task<ApiResponse<bool>> JoinRoom(string roomCode, int userId, string userName)
    {
        // 找到房間
        var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
        if (room == null || room.RoomStatus == RoomStatusEnum.已關閉)
        {
            var errors = new Dictionary<string, string[]> { { "RoomCode", new[] { "找不到房間！" } } };
            return ApiResponseHelper.RequestError<bool>(errors);
        }

        // 檢查是否已在這個房間
        var player = await context.RoomPlayers.FirstOrDefaultAsync(r =>
            r.RoomId == room.RoomId && r.PlayerId == userId
        );
        if (player != null)
        {
            player.IsOnline = 1;
            await context.SaveChangesAsync();
            return ApiResponseHelper.Success(true);
        }

        // 檢查房間狀態
        if (room.RoomStatus != RoomStatusEnum.等待中)
        {
            var errors = new Dictionary<string, string[]> { { "RoomStatus", new[] { "遊戲已開始，無法加入！" } } };
            return ApiResponseHelper.RequestError<bool>(errors);
        }
        // 檢查房間人數上限（最多 6 人）
        int playerCount = await context.RoomPlayers.CountAsync(r => r.RoomId == room.RoomId);
        if (playerCount >= 6)
        {
            var errors = new Dictionary<string, string[]> { { "RoomId", new[] { "房間已滿！" } } };
            return ApiResponseHelper.RequestError<bool>(errors);
        }

        var hostPlayer = new RoomPlayer
        {
            RoomId = room.RoomId,
            PlayerId = userId,
            PlayerName = userName,
            Score = 0,
            DrawOrder = 1,
            IsOnline = 1,
            IsReady = 0,
            JoinTime = DateTime.UtcNow,
        };

        await context.RoomPlayers.AddAsync(hostPlayer);
        await context.SaveChangesAsync();

        return ApiResponseHelper.Success(true);
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    /// <param name="roomId">房間 ID</param>
    /// <param name="userId">玩家 ID</param>
    /// <returns>是否成功</returns>
    public async Task<ApiResponse<bool>> LeaveRoom(int roomId, int userId)
    {
        var player = await context.RoomPlayers.FirstOrDefaultAsync(r => r.RoomId == roomId && r.PlayerId == userId);

        if (player == null)
        {
            var errors = new Dictionary<string, string[]> { { "Player", new[] { "該玩家不在房間中！" } } };
            return ApiResponseHelper.RequestError<bool>(errors);
        }

        context.RoomPlayers.Remove(player);

        await context.SaveChangesAsync();
        return ApiResponseHelper.Success(true);
    }
}
