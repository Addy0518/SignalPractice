namespace Lab.Accounting.API.Infrastructures.ExceptionHandler;

public class InternalServerExceptionHandler : IExceptionHandler
{
    // ========================================================
    // 【IExceptionHandler 是什麼？】
    // ASP.NET Core 8 新增的介面，用來實作全域例外處理
    // 當應用程式任何地方拋出未捕捉的例外，會呼叫這個 Handler
    // 相比舊的 UseExceptionHandler(app => app.Run(...)) 寫法，更乾淨
    //
    // 【為什麼需要全域例外處理？】
    // 不可能在每個地方都 try-catch
    // 全域 Handler 負責：
    // 1. 捕捉所有未處理的例外
    // 2. 記錄 Log
    // 3. 回傳統一格式的錯誤 Response，而不是讓 ASP.NET Core 回傳預設的 HTML 錯誤頁
    // ========================================================
    public async ValueTask<bool> TryHandleAsync(
        // http 訊息
        HttpContext httpContext,
        // 捕捉到的例外
        Exception exception,
        // 當客戶端中斷連線時，CancellationToken 會被觸發 , 傳給非同步操作，讓它們知道可以提早結束
        CancellationToken cancellationToken
    )
    {
        // 例外的簡短訊息（例如："Object reference not set to an instance of an object"）
        var title = exception.Message;

        // 例外的完整資訊，包含 Stack Trace（程式呼叫堆疊，顯示例外發生在哪一行）
        var details = exception.ToString();

        var problemDetails = new ProblemDetails
        {
            // 錯誤的類型識別（這裡用例外的類別名稱）
            Type = exception.GetType().Name,

            // HTTP 狀態碼（500）
            Status = StatusCodes.Status500InternalServerError,

            // 錯誤訊標題
            Title = title,

            // 詳細的錯誤資訊（包含 Stack Trace）
            Detail = details,

            // 發生錯誤的 HTTP 請求路徑（方便定位是哪支 API 出錯）
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
        };

        //  ProblemDetails 還有一個 Extensions 字典，可以加入額外的自訂欄位
        // requestId: ASP.NET Core 自動產生的請求 ID，每個請求的唯一碼 , 用來在 Log 系統中追蹤同一個請求的所有 Log
        // traceId   : 分散式追蹤 ID（Distributed Tracing） , 在微服務架構中，用來追蹤跨服務的請求鏈 , Activity.Current?.Id 取得當前的追蹤 ID
        problemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);
        problemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id);

        // 設定 HTTP Response 狀態碼為 500
        httpContext.Response.StatusCode = problemDetails.Status.Value;

        // 把包裝過的 ProblemDetails 用我們統一的 ApiResponse 格式回傳
        await httpContext.Response.WriteAsJsonAsync(
            ApiResponseHelper.InternalException(problemDetails),
            cancellationToken
        );

        // 回傳 true 表示例外已被處理
        return true;
    }
}
