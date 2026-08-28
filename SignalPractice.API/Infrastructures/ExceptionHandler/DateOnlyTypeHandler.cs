using static Dapper.SqlMapper;

namespace Lab.Accounting.API.Infrastructures.ExceptionHandler
{
    //  繼承 Dapper 的 TypeHandler<T> , 幫助 Dapper 識別 DateOnly 型別
    public class DateOnlyTypeHandler : TypeHandler<DateOnly>
    {
        // 從資料庫讀出原始值 ( Datetime ) 轉成 DateOnly
        public override DateOnly Parse(object value)
        {
            // 因為 Dapper 讀資料庫的 Date 欄位時會讀成 DateTime，所以要轉成 DateOnly 回傳
            return DateOnly.FromDateTime((DateTime)value);
        }

        // 要存進資料庫時呼叫，把 DateOnly 轉成 DateTime , Dapper 才看得懂
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            // DateOnly 沒有時間，ToDateTime 補上 TimeOnly.MinValue（00:00:00）轉成 DateTime 存入
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }
    }
}
