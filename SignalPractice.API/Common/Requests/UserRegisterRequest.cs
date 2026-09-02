namespace SignalPractice.API.Common.Requests
{
    public class UserRegisterRequest
    {
        /// <summary>
        /// 帳號（Email）
        /// </summary>
        [Required(ErrorMessage = "帳號 不能為空!")]
        [EmailAddress(ErrorMessage = "帳號 格式不正確!")]
        [MaxLength(200, ErrorMessage = "帳號 長度不能超過200字!")]
        public string UserAccount { get; set; } = string.Empty;

        /// <summary>
        /// 密碼（8碼，第一個字大寫，含英文和數字）
        /// </summary>
        [Required(ErrorMessage = "密碼 不能為空!")]
        [RegularExpression(
            @"^[A-Z](?=.*[a-zA-Z])(?=.*\d).{7,}$",
            ErrorMessage = "密碼 至少8碼，第一個字須大寫，並包含英文和數字!"
        )]
        public string UserPassword { get; set; } = string.Empty;

        /// <summary>
        /// 使用者姓名（暱稱）
        /// </summary>
        [Required(ErrorMessage = "姓名 不能為空!")]
        [MaxLength(50, ErrorMessage = "姓名 長度不能超過50字!")]
        public string UserName { get; set; } = string.Empty;
    }
}
