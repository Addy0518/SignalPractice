namespace SignalPractice.API.Common.Helpers;

public class TokenHelper(IConfiguration _configuration)
{
    /// <summary>
    /// 創建 Token
    /// </summary>
    /// <param name="userId">使用者的 Id</param>
    /// <param name="userName">使用者的名稱</param>
    /// <param name="userRole">使用者的角色</param>
    /// <param name="userAddress">使用者的地址</param>
    /// <param name="expireMinutes">Token 過期時間（分鐘）</param>
    /// <returns>生成的 JWT Token 字串</returns>
    public string GeneratedToken(
        int userId,
        string userName,
        string? userRole,
        string userAddress,
        int expireMinutes = 30
    )
    {
        // 拿到設定檔的發行人跟鑰匙
        var issuer = _configuration.GetValue<string>("JwtSettings:Issuer");
        var signKey = _configuration.GetValue<string>("JwtSettings:SignKey");

        // 創建一個放 Token 的 key-value 資訊
        // Claim 中文叫「聲明」或「宣告」，就是 Token 裡面夾帶的資訊
        // 每個 Claim 是一個 key-value pair，例如：
        //   UserId = "8"
        //   sub    = "Andy"

        // 這些資訊會被編碼進 Token 中間那段（Payload）
        // 任何人都可以解碼看到內容，但不能偽造（因為有簽名保護）
        var claims = new List<Claim>();

        // 加入自訂的 UserId Claim
        // "UserId" 是自訂的 key 名稱，可以自己取
        // 之後在 Controller 可以用 User.FindFirst("UserId")?.Value 取得
        claims.Add(new Claim("UserId", userId.ToString()));

        // 加入使用者姓名 , Sub 是 jwt 的標準名稱 ( Subject ) , 代表這個 token 是屬於誰的 (這裡是 username) , 筆記裡有寫全套標準名稱
        claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userName));

        if (userRole != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        claims.Add(new Claim("UserAddress", userAddress));

        // 加入  Guid.NewGuid().ToString() ( 唯一碼 ) 到 Jti 裡防止重複
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        // 合併成角色資訊
        // 可以想像成把多張名片（Claim）裝進一個名片夾（ClaimsIdentity）
        // 這個物件後來會被塞進 Token 的 Subject 欄位
        var userClaimsIdentity = new ClaimsIdentity(claims);

        // 建立之前在 appsetting 設定的金鑰 , 用同一把金鑰加密跟解密
        // Encoding.UTF8.GetBytes 是把字串轉成 byte[]，因為加密需要 byte[] 類型的金鑰

        // 對稱（SymmetricSecurityKey）：同一把鑰匙加解密，速度快，適合單一系統
        // 非對稱（RSA）：公鑰加密、私鑰解密，適合多個系統互相驗證
        // 一般中小型專案用對稱就夠了
        var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));

        // 使用 HmacSha256 雜湊處理產生唯一的密鑰
        // HmacSha256Signature 把 Token 的 Header + Payload 用金鑰做雜湊運算，產生簽名
        var signingCredentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256Signature);

        // 建立完整 Token 內容
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // 發行人
            Issuer = issuer,
            // 身分資訊
            Subject = userClaimsIdentity,
            // 過期時間
            Expires = DateTime.Now.AddMinutes(expireMinutes),
            // 簽名方式
            SigningCredentials = signingCredentials,
        };

        // 最後產出 Jwt 物件 ( JwtSecurityTokenHandler ) , 負責產生 , 解析 , 驗證 , 並序列化成字串
        // 1. CreateToken()  → 根據 Descriptor 建立 JWT 物件
        // 2. WriteToken()   → 把 JWT 物件序列化成字串（就是那串看起來亂碼的東西）
        // 3. ReadJwtToken() → 把字串反序列化回 JWT 物件（解析用）
        // 4. ValidateToken()→ 驗證 Token 是否合法
        var tokenHandler = new JwtSecurityTokenHandler();

        // CreateToken：根據 tokenDescriptor 的設定產生 JWT 物件（還不是字串）
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        // WriteToken：把 JWT 物件轉成字串，這才是最終要回傳給前端的 Token
        var serializeToken = tokenHandler.WriteToken(securityToken);

        return serializeToken;
    }
}
