using Microsoft.AspNetCore.Identity.Data;
using SignalPractice.API.Common.Request.User;

namespace SignalPractice.API.Services;

public class UserService(
    SignalPracticeContext context,
    TokenHelper tokenHelper,
    PasswordSecureHelper passwordSecureHelper
) : IUserService
{
    /// <summary>
    /// 使用者註冊
    /// </summary>
    /// <param name="registerRequest">註冊資訊</param>
    /// <returns>是否成功</returns>
    public async Task<ApiResponse<bool>> Register(UserRegisterRequest registerRequest)
    {
        // 檢查帳號是否已存在
        bool isExist = await context.Users.AnyAsync(u => u.UserAccount == registerRequest.UserAccount);
        if (isExist)
        {
            var errors = new Dictionary<string, string[]> { { "UserAccount", new[] { "此帳號已被註冊過！" } } };
            return ApiResponseHelper.RequestError<bool>(errors);
        }

        var user = new User
        {
            UserAccount = registerRequest.UserAccount,
            UserPassword = passwordSecureHelper.HashPassword(registerRequest.UserPassword),
            UserName = registerRequest.UserName,
            RegistrationTime = DateTime.Now,
            IsDelete = 0,
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return ApiResponseHelper.Success(true);
    }

    /// <summary>
    /// 使用者登入
    /// </summary>
    /// <param name="account">帳號</param>
    /// <param name="password">密碼</param>
    /// <returns>使用者資訊</returns>
    public async Task<ApiResponse<UserResponse>> Login(string account, string password)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserAccount == account);

        // 帳號不存在
        if (user == null)
        {
            var errors = new Dictionary<string, string[]> { { "UserAccount", new[] { "帳號或密碼錯誤！" } } };
            return ApiResponseHelper.RequestError<UserResponse>(errors);
        }

        // 驗證密碼
        bool isPasswordCorrect = passwordSecureHelper.VerifyPassword(password, user.UserPassword);
        if (!isPasswordCorrect)
        {
            var errors = new Dictionary<string, string[]> { { "UserPassword", new[] { "帳號或密碼錯誤！" } } };
            return ApiResponseHelper.RequestError<UserResponse>(errors);
        }
        var token = tokenHelper.GeneratedToken(user.UserId, user.UserName, user.UserRole);

        var userResponse = new UserResponse
        {
            UserId = user.UserId,
            UserAccount = user.UserAccount,
            UserName = user.UserName,
            UserBirthDate = user.UserBirthDate,
            UserGender = user.UserGender,
            UserHeadshot = user.UserHeadshot,
            UserRole = user.UserRole,
            UserRegisterMethod = user.UserRegisterMethod,
            RegistrationTime = user.RegistrationTime,
            UpdateTime = user.UpdateTime,
            IsDelete = user.IsDelete,
            DeleteAdminId = user.DeleteAdminId,
            DeleteReason = user.DeleteReason,
            Token = token,
        };
        return ApiResponseHelper.Success(userResponse);
    }

    /// <summary>
    /// 遊客登入
    /// </summary>
    /// <returns>用戶資料</returns>
    public async Task<ApiResponse<UserResponse>> GuestLogin()
    {
        // 生成隨機的使用者ID，範圍為 -10000 到 -99999
        var userId = -Random.Shared.Next(10000, 99999);

        var userName = $"Guest{userId}";

        var token = tokenHelper.GeneratedToken(userId, userName, "Guest");

        var response = new UserResponse
        {
            UserId = userId,
            UserName = userName,
            Token = token,
        };

        return ApiResponseHelper.Success(response);
    }
}
