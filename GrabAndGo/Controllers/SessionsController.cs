namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Shopping-session lifecycle: QR generation and active-session lookup.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SessionsController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public SessionsController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Generate a one-time QR code for store entry.
        /// </summary>
        /// <remarks>
        /// The token is HMAC-SHA256 hashed and expires in 30 minutes. The Flutter app displays the
        /// returned <c>QrCodeData</c> as a QR; the gate scanner reads it and calls <c>POST /api/gate/scan</c>.
        /// </remarks>
        /// <param name="request">Store the user is entering.</param>
        /// <response code="200">QR token generated. Body includes <c>TokenId</c>, <c>ExpiresAt</c>, and <c>QrCodeData</c>.</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpPost("generate-qr")]
        [ProducesResponseType(typeof(QrTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GenerateQr([FromBody] GenerateQrRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User identity could not be verified from token." });
            }

            var result = await _sessionService.GenerateSecureTokenAsync(userId, request.StoreId);
            return Ok(result);
        }

        /// <summary>
        /// Get the user's currently active shopping session, if any.
        /// </summary>
        /// <remarks>
        /// Called by the Flutter app on launch / resume to decide between "scan QR" and "reconnect to live cart".
        /// Returns 204 (not 200 with null) when there's no active session.
        /// </remarks>
        /// <response code="200">User has an active session. Reconnect to SignalR group <c>Session_{SessionId}</c>.</response>
        /// <response code="204">No active session — show "scan QR" UI.</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet("active")]
        [ProducesResponseType(typeof(ActiveSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetActiveSession()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token identity." });
            }

            var active = await _sessionService.GetUserActiveSessionAsync(userId);
            if (active == null)
            {
                return NoContent();
            }
            return Ok(active);
        }
    }
}
