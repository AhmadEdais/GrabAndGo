using GrabAndGo.Models.DTOs;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GrabAndGo.Api.Controllers
{
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

        [HttpPost("generate-qr")]
        public async Task<IActionResult> GenerateQr([FromBody] GenerateQrRequestDto request)
        {
            // 1. Get the UserId from the JWT Claims
            // 'ClaimTypes.NameIdentifier' is the standard place we usually store the ID
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User identity could not be verified from token." });
            }

            // 2. Pass both the verified UserId and the requested StoreId to the service
            var result = await _sessionService.GenerateSecureTokenAsync(userId, request.StoreId);

            return Ok(result);
        }
    }
}
