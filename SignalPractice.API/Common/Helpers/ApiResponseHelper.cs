namespace SignalPractice.API.Helpers;

public static class ApiResponseHelper
{
    // 用靜態方法讓整個專案都能引用
    // T 是型別參數，呼叫時才決定是什麼型別
    // 例如：Success<UserResponse>(data) 或 Success<string>(data)
    // 這樣一個方法可以處理各種型別的資料，不需要為每種型別寫一個方法
    public static ApiResponse<T> Success<T>(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            CodeStatus = ReturnStatusEnum.Success,
            ReturnData = data,
            Message = message,
        };
    }

    // InternalException 是 500 , 通常不會回傳業務資料（ReturnData），只回傳錯誤詳情 , 所以不需要指定資料型別
    // ProblemDetails 是 ASP.NET Core 內建的錯誤描述類別 , 包含：Type, Status, Title, Detail, Instance 等欄位
    // 讓 API 的錯誤格式標準化，方便前端和客戶端處理
    public static ApiResponse<T> InternalException(ProblemDetails errors)
    {
        return new ApiResponse<T>
        {
            CodeStatus = ReturnStatusEnum.InternalException,
            // 就把自訂的 codestatus 的描述訊息當作錯誤訊息回傳就好
            Message = ReturnStatusEnum.InternalException.GetDescription(),
            Error500 = errors,
        };
    }

    // 簡易版 InternalException , 統一回傳伺服器錯誤
    public static ApiResponse<T> InternalException<T>(string detail = "伺服器內部錯誤，請稍後再試")
    {
        return new ApiResponse<T>
        {
            CodeStatus = ReturnStatusEnum.InternalException,
            Message = ReturnStatusEnum.InternalException.GetDescription(),
            Error500 = new ProblemDetails
            {
                Type = "InternalServerError",
                Status = StatusCodes.Status500InternalServerError,
                Title = ReturnStatusEnum.InternalException.GetDescription(),
                Detail = detail,
            },
        };
    }

    // RequestError 是 400 的 Bad Request：客戶端傳來的資料格式或內容有問題
    // 例如：必填欄位沒填、格式錯誤、Token 無效等
    // Dictionary 字典對應 ASP.NET Core ModelState 的驗證錯誤格式：
    // {
    //   "UserAccount": ["帳號不能為空", "帳號長度不能超過50字"],
    //   "UserPassword": ["密碼至少8個字元"]
    // }
    // 一個欄位可以有多個錯誤，所以 value 是 string[]（字串陣列）
    public static ApiResponse<T> RequestError<T>(Dictionary<string, string[]> errors)
    {
        return new ApiResponse<T>
        {
            CodeStatus = ReturnStatusEnum.RequestError,
            ReturnData = default,
            Message = ReturnStatusEnum.RequestError.GetDescription(),
            Error400 = errors,
        };
    }

    public static ApiResponse<T> NotFound<T>()
    {
        return new ApiResponse<T>
        {
            CodeStatus = ReturnStatusEnum.NotFound,
            ReturnData = default,
            Message = ReturnStatusEnum.NotFound.GetDescription(),
        };
    }
}
