namespace SignalPractice.API.Responses;

/// <summary>
/// 統一回復 Response
/// </summary>
/// <typeparam name="T"></typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 狀態碼
    /// </summary>
    public ReturnStatusEnum CodeStatus { get; set; } = ReturnStatusEnum.Success;

    /// <summary>
    /// 訊息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 回傳資料
    /// </summary>
    public T? ReturnData { get; set; } = default!;

    public Dictionary<string, string[]>? Error400 { get; set; } = null;

    public ProblemDetails? Error500 { get; set; } = null;
}
