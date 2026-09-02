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
}
