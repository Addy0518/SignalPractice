namespace SignalPractice.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse<ProblemDetails>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<Dictionary<string, string[]>>))]
    public class UserController(IUserService userService) : ControllerBase
    {
        // 私有方法 : 從 Token 取出 UserId
        private int CurrentUserId => int.Parse(User.FindFirst("UserId")?.Value ?? "0");

        /// <summary>
        /// 使用者註冊
        /// </summary>
        /// <param name="request">註冊資訊</param>
        /// <returns>是否成功</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<bool>))]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            var result = (await userService.Register(request));
            return result.ToActionResult();
        }

        /// <summary>
        /// 使用者登入
        /// </summary>
        /// <param name="loginDTO">登入資訊</param>
        /// <returns>使用者資訊</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserResponse>))]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var result = (await userService.Login(request.Account, request.Password));
            return result.ToActionResult();
        }

        /// <summary>
        /// 遊客登入
        /// </summary>
        /// <returns>用戶資料</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserResponse>))]
        public async Task<IActionResult> GuestLogin()
        {
            var result = (await userService.GuestLogin());
            return result.ToActionResult();
        }

        /// <summary>
        /// 查看個人歷史紀錄
        /// </summary>
        /// <returns>房間資訊</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserResponse>))]
        public async Task<IActionResult> GetGameRecordInfo()
        {
            var result = (await userService.GetGameRecordInfo(CurrentUserId));
            return result.ToActionResult();
        }

        /// <summary>
        /// 查看歷史紀錄的詳細資訊
        /// </summary>
        /// <param name="gameRecordId">遊戲紀錄 ID</param>
        /// <returns>詳細資訊</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<UserResponse>))]
        public async Task<IActionResult> GetRecordDetailsInfo([FromQuery] int gameRecordId)
        {
            var result = (await userService.GetRecordDetailsInfo(gameRecordId, CurrentUserId));
            return result.ToActionResult();
        }
    }
}
