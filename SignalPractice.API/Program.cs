var builder = WebApplication.CreateBuilder(args);

// 加入剛剛設定的 SerilogConfig , 在系統初始化時就先執行 Serilog , 確保系統沒啟動也能夠記錄起來
SerilogConfig.AddSerilLog();

// 預防啟動時出錯
try
{
    // 紀錄 Information 級別的 Log ，包含環境名稱和 URL
    Serilog.Log.Information(
        $"啟動應用程式環境 : {builder.Environment.EnvironmentName} URL: {builder.Configuration["ASPNETCORE_URLS"]}"
    );
    // 開始註冊 DI 服務
    // AddSerilog 是把 Serilog 整合進 ASP.NET Core 的 ILogger 系統，這樣注入 ILogger<T> 的地方實際上會使用 Serilog 來輸出 Log 紀錄
    builder.Services.AddSerilog();

    builder.Services.AddDbContext<SignalPracticeContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DBConnecting"))
    );

    // 註冊 Controller
    builder
        .Services.AddControllers()
        // ConfigureApiBehaviorOptions 是用來自訂 ASP.NET Core 預設的 ModelState 驗證失敗回應行為
        // 這裡要把 requried , maxlength 這些驗證回傳訊息也統一成我自訂的 Apiresponse 格式
        .ConfigureApiBehaviorOptions(options =>
        {
            // InvalidModelStateResponseFactory 自訂驗證訊息工廠
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context
                    // ModelState 裡每個欄位，只取有錯誤的
                    .ModelState.Where(e => e.Value?.Errors.Count > 0)
                    // 把欄位名稱當作 key，錯誤訊息陣列當作 value，組成一個新的 Dictionary<string, string[]>
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                var response = new ApiResponse<object>
                {
                    CodeStatus = ReturnStatusEnum.RequestError,
                    Message = "驗證失敗",
                    Error400 = errors,
                };

                return new BadRequestObjectResult(response);
            };
        });

    // NSwag 套件，產生 OpenAPI（Swagger）文件 , 會掃描所有 Controller 和 Action，自動產生 API 文件 , 就可以在 Swagger UI 上看到
    builder.Services.AddOpenApiDocument(options =>
    {
        options.PostProcess = document =>
        {
            // 設定 Swagger 文件的基本資訊，這些資訊會顯示在 Swagger UI 的頁面上
            document.Info = new OpenApiInfo
            {
                // 可以放 Swagger 頁面標題跟描述
                Version = "v1",
                Title = "練習專案",
                Description = "你畫我猜",
            };
        };
        // AddSecurity 是在 Swagger 文件中加入安全定義，這樣在 Swagger UI 上就會有一個輸入 Token 的欄位，讓使用者可以輸入 JWT Token 來測試需要授權的 API
        options.AddSecurity(
            "Bearer",
            new OpenApiSecurityScheme
            {
                // 告訴 Swagger 這個 API 用 HTTP Bearer Token 驗證
                Type = OpenApiSecuritySchemeType.Http,

                // Swagger 送請求時會自動加上 Authorization: Bearer {輸入的 Token}
                Scheme = "Bearer",

                // 告訴 Swagger 這個 Token 是 JWT 格式的
                BearerFormat = "JWT",

                // 描述說明
                Description = "輸入 Token ",
            }
        );
        // 讓所有需要 [Authorize] 的 API 在 Swagger 文件中標記需要 Bearer Token
        options.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
    });
    // 啟用 CORS 設定
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowVueApp",
            policy =>
            {
                policy
                    //  只允許來自這個來源的請求，其他來源會被瀏覽器阻擋
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://localhost",
                        "https://veneering-bannister-outlook.ngrok-free.dev",
                        "http://www.drawandguess.com",
                        "https://www.drawandguess.com"
                    )
                    // 允許任何 HTTP 方法（GET、POST、PUT、DELETE 等）
                    .AllowAnyMethod()
                    // 允許任何 HTTP 標頭（Header）
                    .AllowAnyHeader()
                    // 允許攜帶 Cookie 或其他認證資訊，這樣前端才能夠在跨域請求中使用認證
                    .AllowCredentials();
                ;
            }
        );
    });

    // 速率限制 , 用來防止 API 被濫用或重複呼叫
    builder.Services.AddRateLimiter(options =>
    {
        // 擋下來的話就回傳 429 狀態碼 ( 太多請求 )
        options.RejectionStatusCode = 429;

        // AddPolicy：註冊一條「具名規則」，"api" 是這條規則的名字
        // 之後在 Controller 上用 [EnableRateLimiting("api")] 就是指定套用這一條
        options.AddPolicy(
            "api",
            httpContext =>
                // GetFixedWindowLimiter 建立一個固定時間窗口限制器 , 意思就是每一段時間被切成一個窗口 ( 這裡設定 10 秒一段 )
                // 每個窗口內最多允許 10 次請求，超過就會被拒絕
                RateLimitPartition.GetFixedWindowLimiter(
                    // partitionKey 是用來區分不同的限制器實例，這裡用呼叫者的 IP 來區分
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    // factory：當某個 partitionKey（某個 IP）第一次出現時，
                    // 要用什麼設定幫它建立一個新的限制器，_ 代表「這個參數沒用到，故意忽略」
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        // Window：這個固定窗口的時間長度是多久
                        Window = TimeSpan.FromSeconds(10),

                        // PermitLimit：在一個窗口時間內，最多允許通過幾次請求
                        PermitLimit = 10,

                        // QueueLimit：超過上面的限制後，要不要讓請求排隊等待，而不是直接拒絕
                        // 設成 0 代表「不排隊，超過就直接擋掉」
                        // （如果設成例如 5，代表超過額度的請求最多讓 5 個排隊，等額度恢復後依序處理，而不是馬上回 429）
                        QueueLimit = 0,
                    }
                )
        );

        // 第二條規則，名字叫 "forgetPassword"，給忘記密碼相關的 API 專用
        // 邏輯結構跟上面一模一樣，只是數字設定不同（更嚴格：時間拉長、次數變少）
        options.AddPolicy(
            "forgetPassword",
            httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // 呼叫者的 IP
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 5,
                        QueueLimit = 0,
                    }
                )
        );
    });

    builder
        .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // 有錯誤就會顯示詳細原因
            options.IncludeErrorDetails = true;

            // 這裡是設定 JWT 驗證的參數，告訴系統如何驗證收到的 JWT Token 是否有效
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // 驗證 Token 的 Issuer（發行人）是否符合設定 , 如果是其他系統發的 token 就拒絕
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),

                // Audience（接收者）驗證，這裡設為不驗證
                // Audience 用來限制 Token 只能給特定的 API 使用
                // 例如：一個 Token 只給 accounting-api 用，不能給 user-api 用
                // 目前關掉表示任何服務都可以用這個 Token
                ValidateAudience = false,
                ValidAudience = "JwtAuthDemo",

                // 驗證 Token 是否過期
                // 過期的 Token 會被拒絕，回傳 401
                ValidateLifetime = true,

                // 驗證 Token 的簽名是否正確
                // 用 IssuerSigningKey 提供的金鑰來驗證簽名
                ValidateIssuerSigningKey = true,

                // 建立之前在 appsetting 設定的金鑰 , 用同一把金鑰加密跟解密
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey"))
                ),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gameHub"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
            };
        });

    // AddExceptionHandler 註測全域例外處理器 , 交給自訂的 InternalServerExceptionHandler 處理
    builder.Services.AddExceptionHandler<InternalServerExceptionHandler>();

    // 加入 ProblemDetails 服務，這樣在發生錯誤時會自動產生符合 RFC 7807 標準的錯誤回應，包含錯誤類型、標題、狀態碼和詳細資訊，讓前端更容易解析和顯示錯誤訊息
    builder.Services.AddProblemDetails();

    builder.Services.AddEndpointsApiExplorer();

    // 快取
    builder.Services.AddMemoryCache();

    // 即時通訊
    builder.Services.AddSignalR();

    // 自訂的 DI 設定
    builder.Services.AddDiConfig(builder.Configuration);

    // 根據前面的設定建立 WebApplication 物件
    // Build() 之後就不能再修改 Services 了
    // Build() 之後才能開始設定 Middleware Pipeline
    var app = builder.Build();

    // 在非開發環境下強制 HTTP 請求重導到 HTTPS ( 加密安全 )
    if (!app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("UseHttpsRedirection"))
    {
        app.UseHttpsRedirection();
    }

    // 瀏覽靜態檔案
    app.UseStaticFiles();

    // 設定 Cross-Origin-Opener-Policy 標頭，允許同源的彈出視窗 ( 讓 Google 登入進來 )
    app.Use(
        async (context, next) =>
        {
            context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin-allow-popups");
            await next();
        }
    );

    // 使用剛剛設定的 Cors
    // 排序上 , 必須在 UseAuthentication、UseAuthorization 之前
    // 否則瀏覽器會先攔截跨域請求，導致認證失敗
    app.UseCors("AllowVueApp");

    // 開啟 OpenAPI JSON 端點（/swagger/v1/swagger.json）
    app.UseOpenApi();

    // 開啟 Swagger UI 頁面（/swagger）
    app.UseSwaggerUI();

    // ========================================================
    // 【Middleware 順序】
    //
    // UseSerilogRequestLogging 要在最外層（最先註冊）
    // 這樣它的「出來」時間點是最晚的，
    // 能確保在 ResponseRequestMiddleware 設定好 ResponseBody 之後才記錄 Log
    //
    // 洋蔥執行順序（由外到內進，由內到外出）：
    // → Serilog（進入）
    //   → ResponseRequestMiddleware（進入，設定 RequestBody）
    //     → ExceptionHandler
    //       → Authentication（驗 JWT）
    //         → TokenBlackListMiddleware（查黑名單）
    //           → Authorization（驗權限）
    //             → Controller（處理請求）
    //           ← Authorization
    //         ← TokenBlackListMiddleware
    //       ← Authentication
    //     ← ExceptionHandler
    //   ← ResponseRequestMiddleware（設定 ResponseBody）
    // ← Serilog（記錄 Log，此時 RequestBody 和 ResponseBody 都已設定好）
    // ========================================================
    app.UseSerilogRequestLogging(opts => opts.EnrichDiagnosticContext = SerilogConfig.EnrichFromRequest);
    // 加入 response 跟 request 讀取 middleware
    app.UseMiddleware<ResponseRequestMiddleware>();
    app.UseExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapHub<GameHub>("/GameHub");

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    // 記錄最高等級的錯誤：系統崩潰、無法繼續執行
    // 如果啟動過程發生未預期的例外，記錄下來方便排查
    Serilog.Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    // 確保所有緩衝中的 Log 都被寫出去再關閉
    // 有些 Sink（例如寫檔案）是有緩衝的，直接結束程式可能會漏掉最後幾筆 Log
    // finally 確保無論正常結束還是例外，都會執行這行
    Serilog.Log.CloseAndFlush();
}
