namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Hardware-only endpoints called by the gate scanner and the exit sensor.
    /// All endpoints require the <c>X-Api-Key</c> header set to <c>HardwareAuth:GateApiKey</c>.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [RequireApiKey("Gate")]   // Both scan and checkout require the X-Api-Key header for the Gate role.
    public class GateController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        private readonly ICheckoutService _checkoutService;

        public GateController(ISessionService sessionService, ICheckoutService checkoutService)
        {
            _sessionService = sessionService;
            _checkoutService = checkoutService;
        }

        /// <summary>
        /// Process a QR code read by the physical gate scanner. Validates the token cryptographically
        /// (Phase A) then atomically opens a Session and Cart in the database (Phase B). A 200 response
        /// signals the turnstile to open; any non-200 keeps the gate closed.
        /// </summary>
        /// <param name="request">QR string scanned from the customer's phone.</param>
        /// <response code="200">Gate opens. Body includes <c>SessionId</c> and <c>CartId</c>.</response>
        /// <response code="400">Request body failed validation, or session creation failed for an unspecified reason.</response>
        /// <response code="401">QR token is invalid, expired, already consumed, or the X-Api-Key is missing/wrong.</response>
        /// <response code="409">User already has another active session — they must check out of the existing one first.</response>
        /// <response code="500">Unexpected server error.</response>
        /// <response code="503">Server is missing the GateApiKey configuration.</response>
        [HttpPost("scan")]
        [ProducesResponseType(typeof(GateEntryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> ScanQrCode([FromBody] GateEntryRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                GateEntryResponseDto? response = await _sessionService.ProcessStoreEntryAsync(request.QrCodeData);
                if (response == null)
                {
                    return BadRequest(new { message = "Failed to open session." });
                }
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("active shopping session"))
                {
                    return Conflict(new { message = ex.Message });
                }
                return StatusCode(500, new { message = "An error occurred while processing entry.", details = ex.Message });
            }
        }

        /// <summary>
        /// Process a customer exit detected by the exit sensor / vision system. Calculates the cart
        /// total, deducts the wallet, creates a Transaction + invoice stub, and decides whether to open
        /// the gate. <b>Always</b> returns HTTP 200 — the gate hardware reads the <c>GateAction</c> field
        /// (<c>"OpenGate"</c> or <c>"KeepClosed"</c>) to decide. This protects the gate from being left
        /// in an error state.
        /// </summary>
        /// <param name="request">TrackId, CameraCode, EventTime from the exit sensor.</param>
        /// <response code="200">Always — even on insufficient funds or server error. Inspect <c>GateAction</c>.</response>
        /// <response code="401">X-Api-Key missing or wrong.</response>
        /// <response code="503">Server is missing the GateApiKey configuration.</response>
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(CheckoutVisionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> ProcessExitEvent([FromBody] CheckoutVisionRequestDto request)
        {
            try
            {
                var result = await _checkoutService.ProcessCheckoutAsync(request);
                return Ok(result);
            }
            catch (Exception)
            {
                // Fallback to ensure the gate stays locked on catastrophic failure.
                return StatusCode(500, new CheckoutVisionResponseDto
                {
                    GateAction = "KeepClosed",
                    Message = "Critical server error.",
                    IsSuccess = false,
                    ShortfallAmount = 0
                });
            }
        }
    }
}
