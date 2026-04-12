using GrabAndGo.Models.Requests.Wallet;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrabAndGo.Api.Controllers
{
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
        [HttpPost("top-up")]
        public async Task<IActionResult> TopUpWallet([FromBody] TopUpRequestDto request)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
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
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            // 1. Securely extract UserId from the JWT Claim
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid token identity." });
            }

            try
            {
                // 2. Fetch the balance
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
    }
}
