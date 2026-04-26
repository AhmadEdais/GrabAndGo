namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// Read endpoints over the authenticated user's transaction history.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        /// <summary>
        /// Get paginated transactions belonging to the authenticated user, most recent first.
        /// </summary>
        /// <param name="page">1-based page number. Defaults to 1.</param>
        /// <param name="pageSize">Items per page. Defaults to 20, capped at 100.</param>
        /// <response code="200">Returns array of transactions (possibly empty).</response>
        /// <response code="401">JWT missing or invalid.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<TransactionListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token identity." });

            var result = await _transactionService.GetUserTransactionsAsync(userId, page, pageSize);
            return Ok(result);
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out userId);
        }
    }
}
