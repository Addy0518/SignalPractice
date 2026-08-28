using Microsoft.Extensions.Caching.Memory;

namespace SignalPractice.Common.Helpers
{
    public class VerifyCodeHelper(IMemoryCache cache)
    {
        private const string VerfiyCodeKeyPrefix = "VerifyCode_";

        /// <summary>
        /// 設定驗證碼到快取
        /// </summary>
        /// <param name="userAccount">使用者帳號</param>
        /// <param name="code">驗證碼</param>
        /// <param name="expiredTime">過期時間</param>
        /// <returns></returns>
        public void SetCode(string userAccount, string code, TimeSpan expiredTime)
        {
            cache.Set(VerfiyCodeKeyPrefix + userAccount, code, expiredTime);
        }

        /// <summary>
        /// 嘗試拿取快取中的驗證碼
        /// </summary>
        /// <param name="userAccount">使用者帳號</param>
        /// <param name="code">驗證碼</param>
        /// <returns>是否成功取得驗證碼</returns>
        public bool TryGetCode(string userAccount, out string? code)
        {
            return cache.TryGetValue(VerfiyCodeKeyPrefix + userAccount, out code);
        }

        /// <summary>
        /// 移除快取中的驗證碼
        /// </summary>
        /// <param name="userAccount">使用者帳號</param>
        /// <returns></returns>
        public void RemoveCode(string userAccount)
        {
            cache.Remove(VerfiyCodeKeyPrefix + userAccount);
        }
    }
}
