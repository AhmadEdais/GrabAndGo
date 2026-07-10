namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Hardware-only endpoint called by the vision edge device to bind a tracked person to a Session.
    /// Requires the <c>X-Api-Key</c> header set to <c>HardwareAuth:VisionApiKey</c>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [RequireApiKey("Vision")]
    public class VisionSystemController : ControllerBase
    {
        private readonly IVisionSystemService _visionSystemService;

        public VisionSystemController(IVisionSystemService visionSystemService)
        {
            _visionSystemService = visionSystemService;
        }

        /// <summary>
        /// Associate a vision-system <c>TrackId</c> with a digital <c>SessionId</c>. Subsequent vision
        /// events emitted under that TrackId will be routed to the bound session's cart. Re-binding a
        /// session retires any previous binding (<c>IsCurrent</c> = 0).
        /// </summary>
        /// <param name="request">SessionId, TrackId, and the source camera/zone identifier.</param>
        /// <response code="200">Track bound. Body includes <c>BindingId</c>.</response>
        /// <response code="400">Request body failed validation.</response>
        /// <response code="401">X-Api-Key missing or wrong.</response>
        /// <response code="404">Bind failed — typically because the SessionId doesn't exist or isn't active.</response>
        /// <response code="500">Unexpected server error.</response>
        /// <response code="503">Server is missing the VisionApiKey configuration.</response>
        [HttpPost("bind-track")]
        [ProducesResponseType(typeof(BindTrackResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<BindTrackResponseDto>> BindTrack([FromBody] BindTrackRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var response = await _visionSystemService.BindTrackAsync(request);
                if (response == null)
                {
                    return NotFound("Failed to bind track. Please check the provided data.");
                }
                return Ok(response);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
