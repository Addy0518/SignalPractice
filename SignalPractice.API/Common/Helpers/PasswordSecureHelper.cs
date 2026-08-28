namespace SignalPractice.API.Common.Helpers;

public class PasswordSecureHelper
{
    /// <summary>
    /// 給密碼進行加密
    /// </summary>
    /// <param name="password">密碼</param>
    /// <returns>加密後的密碼</returns>
    public string HashPassword(string password)
    {
        // 記得先安裝套件BCrypt.Net-Next
        // BCrypt.HashPassword 會自動生成一個隨機的 Salt，並將其嵌入到雜湊字串中。
        // 第三個參數 'workFactor' (或 cost factor) 決定了雜湊的強度。
        // 預設值是 10，更高的值會使雜湊更慢，安全性更高，但會佔用更多 CPU。

        // 使用 BCrypt.Net 簡潔的語法：
        // 'true' 參數代表啟用 Salt (這是預設且推薦的做法)
        return BCrypt.Net.BCrypt.HashPassword(password, 10);
    }

    /// <summary>
    /// 密碼驗證
    /// </summary>
    /// <param name="password">使用者輸入的密碼</param>
    /// <param name="hashedPassword">資料庫中的加密密碼</param>
    /// <returns>是否驗證成功</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        // 使用 BCrypt.Net 的 Verify 方法來驗證密碼
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
