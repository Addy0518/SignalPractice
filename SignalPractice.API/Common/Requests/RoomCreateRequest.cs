namespace SignalPractice.API.Common.Requests
{
    public class RoomCreateRequest
    {
        /// <summary>
        /// 房間名稱
        /// </summary>
        [Required(ErrorMessage = "房間名稱 不能為空!")]
        [MaxLength(50, ErrorMessage = "房間名稱 長度不能超過50字!")]
        public string RoomName { get; set; } = string.Empty;

        /// <summary>
        /// 總輪數
        /// </summary>
        [Required(ErrorMessage = "總輪數 不能為空!")]
        [Range(1, 20, ErrorMessage = "總輪數 必須介於1~20之間!")]
        public int TotalRound { get; set; }

        /// <summary>
        /// 每輪秒數
        /// </summary>
        [Required(ErrorMessage = "每輪秒數 不能為空!")]
        [Range(10, 300, ErrorMessage = "每輪秒數 必須介於10~300秒之間!")]
        public int RoundSeconds { get; set; }
    }
}
