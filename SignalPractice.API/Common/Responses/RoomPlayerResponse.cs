namespace SignalPractice.API.Common.Responses
{
    public class RoomPlayerResponse
    {
        /// <summary>
        /// 玩家 ID
        /// </summary>
        public int PlayerId { get; set; }

        /// <summary>
        /// 玩家姓名
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// 分數
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 畫畫順序
        /// </summary>
        public int DrawOrder { get; set; }

        /// <summary>
        /// 是否在線（1：在線 / 0：不在線）
        /// </summary>
        public int IsOnline { get; set; }

        /// <summary>
        /// 是否準備完成（1：已準備 / 0：未準備）
        /// </summary>
        public int IsReady { get; set; }
    }
}
