using SignalPractice.API.Common.Responses;

namespace SignalPractice.API.Common.Extensions;

public static class ApiResponseExtensions
{
    /// <summary>
    /// 把自訂的 ApiResponse&lt;T&gt; 轉成對應的 HTTP 狀態碼結果
    /// 讓 CodeStatus 是 400/404/500 時，HTTP 狀態碼也會跟著回傳正確的值
    /// 而不是每次都固定回傳 200
    /// </summary>
    public static IActionResult ToActionResult<T>(this ApiResponse<T> response)
    {
        return response.CodeStatus switch
        {
            ReturnStatusEnum.Success => new OkObjectResult(response),
            ReturnStatusEnum.RequestError => new BadRequestObjectResult(response),
            ReturnStatusEnum.NotFound => new NotFoundObjectResult(response),
            ReturnStatusEnum.InternalException => new ObjectResult(response)
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            },
            // 沒有對應到的狀態，預設回傳 200，避免遺漏 case 時整支 API 掛掉
            _ => new OkObjectResult(response),
        };
    }
}
