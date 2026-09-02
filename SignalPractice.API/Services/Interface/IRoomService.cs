namespace SignalPractice.API.Services;

public interface IRoomService
{
    /// <summary>
    /// 查看房間資訊
    /// </summary>
    /// <param name="roomCode">房間代碼</param>
    /// <param name="userId">玩家 ID</param>
    /// <returns>房間資訊</returns>
    Task<ApiResponse<RoomResponse>> GetRoomInfo(string roomCode, int userId);

    /// <summary>
    /// 創建房間
    /// </summary>
    /// <param name="request">房間資訊</param>
    /// <param name="userId">創建者 ID</param>
    /// <param name="userName">創建者姓名</param>
    /// <returns>房間代碼</returns>
    Task<ApiResponse<string>> CreateRoom(RoomCreateRequest request, int userId, string userName);

    /// <summary>
    /// 加入房間
    /// </summary>
    /// <param name="roomCode">房間代碼</param>
    /// <param name="userId">玩家 ID</param>
    /// <param name="userName">玩家姓名</param>
    /// <returns>是否成功</returns>
    Task<ApiResponse<bool>> JoinRoom(string roomCode, int userId, string userName);

    /// <summary>
    /// 離開房間
    /// </summary>
    /// <param name="roomId">房間 ID</param>
    /// <param name="userId">玩家 ID</param>
    /// <returns>是否成功</returns>
    Task<ApiResponse<bool>> LeaveRoom(int roomId, int userId);
}
