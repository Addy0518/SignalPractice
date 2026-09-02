namespace SignalPractice.API.Common.Requests
{
    public class UserLoginRequest
    {
        /// <summary>
        /// 帳號
        /// </summary>
        [Required(ErrorMessage = "帳號 不能為空!")]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 密碼
        /// </summary>
        [Required(ErrorMessage = "密碼 不能為空!")]
        public string Password { get; set; } = string.Empty;
    }
}
