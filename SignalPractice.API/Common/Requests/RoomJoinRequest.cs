namespace SignalPractice.API.Common.Requests
{
    public class RoomJoinRequest
    {
        /// <summary>
        /// 房間加入碼
        /// </summary>
        [Display(Name = "房間代碼")]
        [Required(ErrorMessage = "{0} 不能為空!")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "{0} 必須是 6 碼")]
        public string RoomCode { get; set; }
    }
}
