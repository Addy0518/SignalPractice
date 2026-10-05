namespace SignalPractice.API.Services.Interface;

public interface IUserService
{
    /// <summary>
    /// 使用者註冊
    /// </summary>
    /// <param name="registerRequest">註冊資訊</param>
    /// <returns>是否成功</returns>
    Task<ApiResponse<bool>> Register(UserRegisterRequest registerRequest);

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="account">帳號</param>
    /// <param name="password">密碼</param>
    /// <returns>使用者資訊</returns>
    Task<ApiResponse<UserResponse>> Login(string account, string password);

    /// <summary>
    /// 遊客登入
    /// </summary>
    /// <returns>用戶資料</returns>
    Task<ApiResponse<UserResponse>> GuestLogin();

    /// <summary>
    /// 查看個人歷史紀錄
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>房間資訊</returns>
    Task<ApiResponse<List<UserRecordResponse>>> GetGameRecordInfo(int userId);

    /// <summary>
    /// 查看歷史紀錄的詳細資訊
    /// </summary>
    /// <param name="gameRecordId">遊戲紀錄 ID</param>
    /// <param name="userId">使用者 ID</param>
    /// <returns>詳細資訊</returns>
    Task<ApiResponse<RecordDetailsResponse>> GetRecordDetailsInfo(int gameRecordId, int userId);
}
