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

    /// <summary>
    /// 查看個人歷史紀錄
    /// </summary>
    /// <param name="userId">使用者 ID</param>
    /// <returns>房間資訊</returns>
    public async Task<ApiResponse<List<UserRecordResponse>>> GetGameRecordInfo(int userId)
    {
        var allRecords = await context
            .GameRecordPlayers.Where(p => p.PlayerId == userId)
            .OrderByDescending(x => x.GameRecord.CreateTime)
            .Select(p => new UserRecordResponse
            {
                GameRecordId = p.GameRecordId,
                RoomName = p.GameRecord.RoomName,
                TotalRound = p.GameRecord.TotalRound,
                CreateTime = p.GameRecord.CreateTime,
                Score = p.Score,
                Rank = p.Rank,
            })
            .Take(50)
            .ToListAsync();

        return ApiResponseHelper.Success(allRecords);
    }

    /// <summary>
    /// 查看歷史紀錄的詳細資訊
    /// </summary>
    /// <param name="gameRecordId">遊戲紀錄 ID</param>
    /// <param name="userId">使用者 ID</param>
    /// <returns>詳細資訊</returns>
    public async Task<ApiResponse<RecordDetailsResponse>> GetRecordDetailsInfo(int gameRecordId, int userId)
    {
        var record = await context
            .GameRecords.Where(r => r.GameRecordId == gameRecordId)
            .Select(r => new RecordDetailsResponse
            {
                GameRecordId = r.GameRecordId,
                RoomName = r.RoomName,
                TotalRound = r.TotalRound,
                RoundSeconds = r.RoundSeconds,
                CreateTime = r.CreateTime,
                Players = r
                    .GameRecordPlayers.OrderBy(p => p.Rank)
                    .Select(p => new RecordPlayerResponse
                    {
                        PlayerId = p.PlayerId,
                        PlayerName = p.PlayerName,
                        Score = p.Score,
                        Rank = p.Rank,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync();

        if (record == null || !record.Players.Any(p => p.PlayerId == userId))
        {
            var errors = new Dictionary<string, string[]> { { "GameRecord", new[] { "查無此場紀錄！" } } };
            return ApiResponseHelper.RequestError<RecordDetailsResponse>(errors);
        }

        return ApiResponseHelper.Success(record);
    }
}
