namespace Lab.Accounting.API.Infrastructures.Logging;

public class SerilogConfig
{
    public static void AddSerilLog()
    {
        var columnOptions = new Serilog.Sinks.MSSqlServer.ColumnOptions();

        columnOptions.AdditionalColumns = new List<SqlColumn>
        {
            new SqlColumn
            {
                ColumnName = "RequestBody",
                DataType = SqlDbType.NVarChar,
                DataLength = -1,
            },
            new SqlColumn
            {
                ColumnName = "ResponseBody",
                DataType = SqlDbType.NVarChar,
                DataLength = -1,
            },
            new SqlColumn
            {
                ColumnName = "RemoteIp",
                DataType = SqlDbType.NVarChar,
                DataLength = 50,
            },
        };

        // ========================================================
        // 【LoggerConfiguration 是什麼？】
        // Serilog 的設定建構器，用 Fluent API（鏈式呼叫）來設定
        // 最後呼叫 CreateLogger() 才真正建立 Logger 實例
        // ========================================================

        var loggerConfiguration = new LoggerConfiguration()
            // 設定全域最低 Log 輸出等級 , 設為 Information 表示 Debug 和 Verbose 等級的 Log 不會輸出
            .MinimumLevel.Information()
            // ========================================================
            // 【MinimumLevel.Override() 針對特定命名空間的來源，覆蓋全域的最低等級設定
            // ASP.NET Core 框架本身會產生大量 Information 等級的 Log
            // 例如：每個靜態檔案請求、路由解析過程等
            // 這些通常不是我們想看的，設為 Warning 可以過濾掉雜訊
            // ========================================================
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            // 允許在執行過程中動態把屬性加進 Log
            .Enrich.FromLogContext() // 可以增加Log輸出欄位 https://www.cnblogs.com/wd4j/p/15043489.html
            // WithExceptionDetails 是當 Log 包含 Exception 時，自動把例外的詳細資訊（InnerException、 自訂屬性等）展開記錄，而不只是 ToString() 的結果
            .Enrich.WithExceptionDetails()
            // WithProperty 在每一筆 Log 都自動加上這個固定屬性
            // 當你有多個服務都把 Log 送到同一個 Seq 時，
            // 用 Application 欄位就能區分是哪個服務的 Log
            .Enrich.WithProperty("Application", "Lab.Accounting.API")
            // 把 Log 送到 Seq 伺服器
            .WriteTo.Seq("http://localhost:5341")
            // 把 Log 輸出到 Console，並使用 AnsiConsoleTheme.Code 主題來美化輸出
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                // ========================================================
                // 【outputTemplate 是什麼？】
                // 定義 Console 輸出的格式字串
                // {Timestamp:yyyy/MM/dd HH:mm:ss} → 時間戳記
                // {Level:u3}  → Log 等級，3個字母縮寫（INF, WRN, ERR）
                // {Message:lj} → Log 訊息，lj = Literal with JSON for complex objects
                // {NewLine}   → 換行
                // {RequestBody} → 我們自訂加進去的欄位（從 Enricher 來的）
                // {ResponseBody} → 同上
                // {Exception} → 例外資訊（如果有的話）
                // ========================================================
                outputTemplate: "[{Timestamp:yyyy/MM/dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{RequestBody}{ResponseBody}{NewLine}{Exception}"
            )
            .WriteTo.MSSqlServer(
                connectionString: "Data Source=localhost\\SQLEXPRESS;Initial Catalog=AccountPractice;User ID=angey920518;Password=Andy920518!;TrustServerCertificate=True;",
                sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                {
                    TableName = "Logs",
                    AutoCreateSqlDatabase = true,
                    AutoCreateSqlTable = true,
                },
                columnOptions: columnOptions,
                restrictedToMinimumLevel: LogEventLevel.Error
            );

        // ========================================================
        // 【Log.Logger 是什麼？】
        // Serilog 的全域靜態 Logger
        // 設定好後整個應用程式都可以用 Log.Information("...") 來記錄
        // CreateLogger() 根據上面的設定建立實際的 Logger 物件
        // ========================================================

        Serilog.Log.Logger = loggerConfiguration.CreateLogger();
    }

    // ========================================================
    // 【EnrichFromRequest 的目的】
    // 這是一個 callback 方法，在每個 HTTP 請求結束時被 Serilog 呼叫
    // 把我們想額外記錄的資訊（RequestBody、ResponseBody、IP 等）加進這次請求的 Log 紀錄
    // ========================================================
    public static async void EnrichFromRequest(
        // Serilog 提供的，用來設定額外 Log 欄位
        IDiagnosticContext diagnosticContext,
        // 當前的 http 資訊
        HttpContext httpContext
    )
    {
        // 拿到 request 的資訊
        var request = httpContext.Request;

        // 這個值是 ResponseRequestMiddleware 在更內層設定的
        // 因為 Serilog Middleware 在最外層，middleware 跑完要運出去就會經過這裡
        // 所以這裡一定可以拿到已經設定好的 request body 跟 response body
        var requestBody = httpContext.Items["RequestBody"]?.ToString() ?? string.Empty;

        // Set 把值放進去
        diagnosticContext.Set("RequestBody", requestBody);

        // response 也是一樣
        var responsebody = httpContext.Items["ResponseBody"]?.ToString() ?? string.Empty;
        diagnosticContext.Set("ResponseBody", responsebody);

        // 設定更多想記錄的欄位

        // 主機名稱
        diagnosticContext.Set("Host", request.Host);

        // http or https
        diagnosticContext.Set("Scheme", request.Scheme);

        // 完整的 HTTP Headers
        diagnosticContext.Set("Headers", request.Headers);

        // 取得客戶端真實 IP
        string ip =
            request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "Unknown";

        diagnosticContext.Set("RemoteIp", ip);

        // 有 querystring 的時候在紀錄 ( 例如：GET /api/ledger?userId=1 , ? 後面就是 querystring (參數字串) )
        if (request.QueryString.HasValue)
        {
            diagnosticContext.Set("QueryString", request.QueryString.Value);
        }

        // 記錄 Response 的 Content-Type（例如 application/json）
        diagnosticContext.Set("ContentType", httpContext.Response.ContentType);

        // GetEndpoint 最後取得這個端點的資訊 , 例如：Lab.Accounting.API.Controllers.UserController.Login (Lab.Accounting.API)
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is object) // endpoint != null
        {
            diagnosticContext.Set("EndpointName", endpoint.DisplayName);
        }
    }
}
