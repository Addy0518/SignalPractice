namespace Lab.Accounting.API.Infrastructures.Logging;

public class ResponseRequestMiddleware
{
    // ========================================================
    // 【這個 Middleware 的目的】
    // 把每個 HTTP 請求的 Request Body 和 Response Body 讀出來
    // 存進 HttpContext.Items，讓 Serilog 可以把這些內容記錄到 Log
    //
    // 【為什麼需要這個 Middleware？】
    // HTTP 的 Body 是 Stream（資料流），特性是「讀完就沒了」
    // 預設情況下 Body 只能讀一次，讀完之後位置就停在結尾，再讀就是空的
    // 這個 Middleware 負責把 Body 截取下來，讓系統可以讀多次
    // ========================================================
    private readonly RequestDelegate _next;

    public ResponseRequestMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        // 用做好的讀取 stream 方法
        string requestBodyPayload = await ReadRequestBody(context.Request);
        // 塞回 request 內部
        context.Items["RequestBody"] = requestBodyPayload;

        // response 也是一樣概念 , 先標記 Body
        var orginalresponse = context.Response.Body;

        // 給他複製一個新物件 stream , 塞進 body
        using (var responsebody = new MemoryStream())
        {
            context.Response.Body = responsebody;
            // 往下一層走
            await _next(context);

            // 把 MemoryStream 裡的 Response 內容讀出來
            string rsponseBodyPayload = await ReadResponseBody(context.Response);

            // 再把原本的 stream 塞回去 , 真的進來被我們變成複製的 , 要出去時再把原本的塞回去的概念
            // 存進 Items 讓 Serilog 可以記錄
            context.Items["ResponseBody"] = rsponseBodyPayload;

            // ========================================================
            // 【還原流程】
            // 1. 把 MemoryStream 的讀取位置移回最開始（Position = 0）
            //    因為 ReadResponseBody 讀完後位置停在結尾，要倒帶才能複製
            // 2. CopyToAsync：把 MemoryStream 的內容複製到原本的 Network Stream
            //    這樣資料才會真正送到客戶端
            // 3. 把 Response.Body 換回原本的 Network Stream
            // ========================================================
            responsebody.Position = 0;
            await responsebody.CopyToAsync(orginalresponse);
            context.Response.Body = orginalresponse;
        }
    }

    /// <summary>
    /// 讀取 httpcontext 裡的請求資訊
    /// </summary>
    private async Task<string> ReadRequestBody(HttpRequest request)
    {
        // EnableBuffering 是允許 request 可以被多次讀取
        request.EnableBuffering();
        // 因為 stream 讀取玩進度就會停在 stream 的結尾 , 所以要先記錄下來待會再把它 "倒帶" 回去
        var body = request.Body;
        // 根據請求的內容長度預留一個型態為 byte 的空間
        var buffer = new byte[Convert.ToInt32(request.ContentLength)];
        // 叫系統把剛剛的內容塞到 buffer 裡 , 待會要讀取 (buffer,開始讀取位置,結束位置)
        await request.Body.ReadAsync(buffer, 0, buffer.Length);
        // 轉換成字串 , 也就是我們能看得懂的
        string requestbody = Encoding.UTF8.GetString(buffer);
        // 將位置調回最開始 0
        body.Seek(0, SeekOrigin.Begin);
        // 傳回剛剛解讀出來得資訊
        return $"{requestbody}";
    }

    private static async Task<string> ReadResponseBody(HttpResponse response)
    {
        // 把 MemoryStream 的讀取位置移到最開始（Controller 寫完後位置在結尾）
        response.Body.Seek(0, SeekOrigin.Begin);

        // StreamReader 把 Stream（byte 資料流）包裝成可以用字串方式讀取的 Reader
        // ReadToEndAsync()：把從目前位置到結尾的所有內容讀出來，回傳字串
        string responseBody = await new StreamReader(response.Body).ReadToEndAsync();

        // 讀完後再 Seek 回去，讓後面的 CopyToAsync 可以從頭複製
        response.Body.Seek(0, SeekOrigin.Begin);

        return $"{responseBody}";
    }
}
