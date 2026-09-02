namespace SignalPractice.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ApiResponse<ProblemDetails>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ApiResponse<Dictionary<string, string[]>>))]
    public class RoomController(IRoomService roomService) : ControllerBase
    {
        private int CurrentUserId => int.Parse(User.FindFirst("UserId")?.Value ?? "0");

        private string CurrentUserName => User.FindFirst("UserName")?.Value ?? string.Empty;

        /// <summary>
        /// 查看房間資訊
        /// </summary>
        /// <param name="roomCode">房間代碼</param>
        /// <returns>房間資訊</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<RoomResponse>))]
        public async Task<IActionResult> GetRoomInfo([FromQuery] string roomCode)
        {
            var result = await roomService.GetRoomInfo(roomCode, CurrentUserId);
            return result.ToActionResult();
        }

        /// <summary>
        /// 創建房間
        /// </summary>
        /// <param name="request">房間資訊</param>
        /// <returns>房間代碼</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<string>))]
        public async Task<IActionResult> CreateRoom([FromBody] RoomCreateRequest request)
        {
            var result = await roomService.CreateRoom(request, CurrentUserId, CurrentUserName);
            return result.ToActionResult();
        }

        /// <summary>
        /// 加入房間
        /// </summary>
        /// <param name="roomCode">房間代碼</param>
        /// <returns>是否成功</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<bool>))]
        public async Task<IActionResult> JoinRoom([FromBody] RoomJoinRequest request)
        {
            var result = await roomService.JoinRoom(request.RoomCode, CurrentUserId, CurrentUserName);
            return result.ToActionResult();
        }

        /// <summary>
        /// 離開房間
        /// </summary>
        /// <param name="roomId">房間 ID</param>
        /// <returns>是否成功</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApiResponse<bool>))]
        public async Task<IActionResult> LeaveRoom([FromQuery] int roomId)
        {
            var result = await roomService.LeaveRoom(roomId, CurrentUserId);
            return result.ToActionResult();
        }
    }
}
