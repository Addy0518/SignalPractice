namespace SignalPractice.API.Common.Responses
{
    public class RoomResponse
    {
        /// <summary>
        /// 房間 ID
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// 房間代碼（6碼，用於加入房間）
        /// </summary>
        public string RoomCode { get; set; } = string.Empty;

        /// <summary>
        /// 房間名稱
        /// </summary>
        public string RoomName { get; set; } = string.Empty;

        /// <summary>
        /// 房主 ID
        /// </summary>
        public int RoomOwnerId { get; set; }

        /// <summary>
        /// 房間狀態（等待中 / 遊戲中 / 已結束）
        /// </summary>
        public RoomStatusEnum RoomStatus { get; set; }

        /// <summary>
        /// 總輪數
        /// </summary>
        public int TotalRound { get; set; }

        /// <summary>
        /// 目前輪數
        /// </summary>
        public int? CurrentRound { get; set; }

        /// <summary>
        /// 目前繪畫的人
        /// </summary>
        public int? CurrentDrawerId { get; set; }

        /// <summary>
        /// 每輪秒數
        /// </summary>
        public int RoundSeconds { get; set; }

        /// <summary>
        /// 本輪結束時間
        /// </summary>
        public DateTime? RoundEndTime { get; set; }

        /// <summary>
        /// 房間內的玩家列表
        /// </summary>
        public List<RoomPlayerResponse> Players { get; set; } = new();
    }
}
