using GrabAndGo.DataAccess.Interfaces;
using GrabAndGo.Models.Requests.Vision_System;
using GrabAndGo.Models.Responses.Vision_System;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace GrabAndGo.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisionSystemController : ControllerBase
    {
        private readonly IVisionSystemService _visionSystemService;

        public VisionSystemController(IVisionSystemService visionSystemService)
        {
            _visionSystemService = visionSystemService;
        }
        [HttpPost("bind-track")]
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}
            

