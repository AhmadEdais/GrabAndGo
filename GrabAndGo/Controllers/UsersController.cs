using GrabAndGo.Models.Requests.Users;
using GrabAndGo.Models.Responses.Users;
using GrabAndGo.Services;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GrabAndGo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        // Inject the Service, not the Repository!
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserRegistrationDto dto)
        {
            // 1. Data Annotation Validation (Checks lengths, email format, etc.)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 2. Hand it off to the Service layer (which will hash the password)
                var response = await _userService.RegisterUserAsync(dto);

                // 3. Return 201 Created, and provide the URL to fetch the new user
                return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, response);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Email is already registered.")
            {
                // Catch the EXACT exception we threw from the Service Layer
                // 409 Conflict is the standard HTTP code for duplicate records
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Catch any other completely unexpected system errors
                return StatusCode(500, new { message = "An error occurred during registration.", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuthResponseDto>> GetUserById(int id)
        {
            // Ask the service layer for the user
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return Ok(user);
        }
        // Add this inside your existing UsersController
        
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Pass off to the service layer
                var response = await _userService.LoginUserAsync(dto);

                // 200 OK + The AuthResponseDto (which now includes the JWT)
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                // 401 Unauthorized is the correct HTTP status for bad passwords
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
            }
        }
    }
}