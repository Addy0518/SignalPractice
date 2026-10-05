namespace SignalPractice.API.Common.Responses
{
    public class UserRecordResponse
    {
        /// <summary>
        /// 遊戲紀錄 ID
        /// </summary>
        public int GameRecordId { get; set; }

        /// <summary>
        /// 房間名稱
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// 總輪數
        /// </summary>
        public int TotalRound { get; set; }

        /// <summary>
        /// 遊戲結束時間
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 分數
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 名次
        /// </summary>
        public int Rank { get; set; }
    }
}
