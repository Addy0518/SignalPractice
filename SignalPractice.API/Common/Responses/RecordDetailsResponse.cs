namespace SignalPractice.API.Common.Responses
{
    public class RecordDetailsResponse
    {
        /// <summary>
        /// 遊戲紀錄 ID
        /// </summary>
        public int GameRecordId { get; set; }

        /// <summary>
        /// 房間名稱（遊戲結束當下的快照）
        /// </summary>
        public string RoomName { get; set; }

        /// <summary>
        /// 總輪數
        /// </summary>
        public int TotalRound { get; set; }

        /// <summary>
        /// 每輪秒數
        /// </summary>
        public int RoundSeconds { get; set; }

        /// <summary>
        /// 遊戲結束時間
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 這場遊戲所有玩家的成績（依名次排序）
        /// </summary>
        public List<RecordPlayerResponse> Players { get; set; } = new();
    }

    public class RecordPlayerResponse
    {
        /// <summary>
        /// 玩家 ID
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// 玩家名稱（遊戲結束當下的快照）
        /// </summary>
        public string PlayerName { get; set; }

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
