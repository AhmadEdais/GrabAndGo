namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Wallet operations: top-up, balance, and ledger history.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WalletsController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletsController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Add funds to the authenticated user's wallet.
        /// </summary>
        /// <param name="request">Amount to add (0.01 – 10,000 JOD).</param>
        /// <response code="200">Top-up applied. Body includes <c>NewBalance</c>.</response>
        /// <response code="400">Top-up failed (e.g., amount out of range, wallet missing).</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpPost("top-up")]
        [ProducesResponseType(typeof(TopUpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User identity could not be verified from token." });
            }

            var result = await _walletService.TopUpWalletAsync(userId, request.Amount);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to top up wallet." });
            }
            return Ok(result);
        }

        /// <summary>
        /// Get the authenticated user's current wallet balance.
        /// </summary>
        /// <response code="200">Returns <c>WalletId</c>, <c>CurrentBalance</c>, <c>LastUpdatedAt</c>.</response>
        /// <response code="401">JWT missing or invalid.</response>
        /// <response code="404">Wallet not found for this user.</response>
        /// <response code="500">Unexpected server error.</response>
        [HttpGet("balance")]
        [ProducesResponseType(typeof(WalletBalanceResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBalance()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token identity." });
            }

            try
            {
                var result = await _walletService.GetWalletBalanceAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred retrieving the balance.", details = ex.Message });
            }
        }

        /// <summary>
        /// Get paginated wallet ledger entries (top-ups, debits, refunds), most recent first.
        /// </summary>
        /// <param name="page">1-based page number. Defaults to 1.</param>
        /// <param name="pageSize">Items per page. Defaults to 20, capped at 100.</param>
        /// <response code="200">Returns array of ledger entries (possibly empty).</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet("ledger")]
        [ProducesResponseType(typeof(List<WalletLedgerEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLedger([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token identity." });
            }

            var result = await _walletService.GetUserWalletLedgerAsync(userId, page, pageSize);
            return Ok(result);
        }
    }
}
