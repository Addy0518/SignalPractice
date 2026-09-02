namespace SignalPractice.API.Common.Responses
{
    public class UserResponse
    {
        /// <summary>
        /// 使用者 ID（遊客為負數）
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 帳號（Email）（遊客為 null）
        /// </summary>
        public string? UserAccount { get; set; }

        /// <summary>
        /// 使用者姓名（暱稱）
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 生日
        /// </summary>
        public DateOnly? UserBirthDate { get; set; }

        /// <summary>
        /// 性別
        /// </summary>
        public GenderEnum UserGender { get; set; }

        /// <summary>
        /// 大頭貼網址
        /// </summary>
        public string? UserHeadshot { get; set; }

        /// <summary>
        /// 使用者角色（一般會員 / Guest 等）
        /// </summary>
        public string? UserRole { get; set; }

        /// <summary>
        /// 註冊方式（帳密 / Google 等）
        /// </summary>
        public RegisterMethodEnum UserRegisterMethod { get; set; }

        /// <summary>
        /// 註冊時間
        /// </summary>
        public DateTime? RegistrationTime { get; set; }

        /// <summary>
        /// 資料最後更新時間
        /// </summary>
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 是否已刪除（1：已刪除 / 0：未刪除）
        /// </summary>
        public int IsDelete { get; set; }

        /// <summary>
        /// 執行刪除的管理員 ID
        /// </summary>
        public int? DeleteAdminId { get; set; }

        /// <summary>
        /// 刪除原因
        /// </summary>
        public string? DeleteReason { get; set; }

        /// <summary>
        /// 登入後發放的 JWT Token
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
