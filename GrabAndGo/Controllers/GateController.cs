using GrabAndGo.Models.DTOs;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GrabAndGo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GateController : ControllerBase
    {
        private readonly ISessionService _sessionService;

        public GateController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        /// <summary>
        /// Simulates the physical gate scanner reading a QR code from a user's phone.
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanQrCode([FromBody] GateEntryRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Execute Phase A (Validation) and Phase B (Transaction)
                GateEntryResponseDto? response = await _sessionService.ProcessStoreEntryAsync(request.QrCodeData);

                if (response == null)
                {
                    return BadRequest(new { message = "Failed to open session." });
                }

                // 200 OK signals the physical hardware to open the turnstile!
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Triggered by Phase A if the hash is wrong, token is expired, or already consumed.
                // Returning 401 Unauthorized flashes a Red Light on the physical gate.
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Triggered by Phase B if the SQL Stored Procedure throws an error 
                // (e.g., Error 50001: "User already has an active shopping session")
                if (ex.Message.Contains("active shopping session"))
                {
                    return Conflict(new { message = ex.Message });
                }

                // Generic fallback for any other database or server issues
                return StatusCode(500, new { message = "An error occurred while processing entry.", details = ex.Message });
            }
        }
    }
}