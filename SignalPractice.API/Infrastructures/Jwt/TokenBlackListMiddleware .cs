namespace Lab.Accounting.API.Infrastructures.Jwt;

public class TokenBlackListMiddleware
{
    private readonly RequestDelegate _next;

    public TokenBlackListMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ITokenBlacklistRepository tokenBlacklistRepositories)
    {
        // 取得 HTTP 請求的 Authorization Header 格式：Authorization: Bearer eyJhbGci... , 把 Bearer 跟後面空白去掉 , 拿 Token 的部分
        var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        // 有 token 就開始解析
        if (!string.IsNullOrEmpty(token))
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // 如果 token 能被解析就執行
            if (tokenHandler.CanReadToken(token))
            {
                // 解析 token 取得 jti (JWT ID)，用來查詢是否在黑名單中
                var jwt = tokenHandler.ReadJwtToken(token);
                var jti = jwt.Id;

                if (await tokenBlacklistRepositories.isBlackList(jti))
                {
                    context.Response.StatusCode = 401;
                    // 把物件序列化成 JSON 並寫入 Response Body
                    await context.Response.WriteAsJsonAsync(new { message = "Token 已失效，請重新登入" });
                    return;
                }
            }
        }

        await _next(context);
    }
}
