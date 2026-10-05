using System.ComponentModel;

namespace SignalPractice.API.Common.Enums;

// 狀態碼
public enum RoomStatusEnum
{
    [Description("等待中")]
    等待中 = 0,

    [Description("遊戲中")]
    遊戲中 = 1,

    [Description("已結束")]
    已結束 = 2,

    [Description("已關閉")]
    已關閉 = 3,
}
