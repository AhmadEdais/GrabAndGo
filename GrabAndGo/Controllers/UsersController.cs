namespace GrabAndGo.Api.Controllers
{
    /// <summary>
    /// User identity and authentication. Registration / login are public; profile reads require a JWT.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Register a new user account. Creates the user and an empty wallet.
        /// </summary>
        /// <param name="dto">First name, last name, email, password (plain — server hashes with BCrypt).</param>
        /// <response code="201">User created. Returns the new user's profile (no JWT — call <c>/login</c> next).</response>
        /// <response code="400">Request body failed validation.</response>
        /// <response code="409">Email is already registered.</response>
        /// <response code="500">Unexpected server error.</response>
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] UserRegistrationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _userService.RegisterUserAsync(dto);
                return CreatedAtAction(nameof(GetUserById), new { id = response.UserId }, response);
            }
            catch (InvalidOperationException ex) when (ex.Message == "Email is already registered.")
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during registration.", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a user's profile. Users may only fetch their own profile (ownership check).
        /// </summary>
        /// <param name="id">User ID. Must match the authenticated user's ID.</param>
        /// <response code="200">Returns the user profile.</response>
        /// <response code="401">JWT missing or unparseable.</response>
        /// <response code="403">Authenticated, but the requested ID is not the caller's.</response>
        /// <response code="404">User does not exist or is inactive.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AuthResponseDto>> GetUserById(int id)
        {
            // 1. Verify token identity
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int jwtUserId))
            {
                return Unauthorized(new { message = "Invalid token identity." });
            }

            // 2. Ownership check: a user can only fetch their own profile.
            if (jwtUserId != id)
            {
                return Forbid();
            }

            // 3. Load and return
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = $"User with ID {id} not found." });
            }

            return Ok(user);
        }

        /// <summary>
        /// Authenticate with email + password. Returns a JWT valid for 7 days.
        /// </summary>
        /// <param name="dto">Email and password.</param>
        /// <response code="200">Authentication succeeded. The response body includes a <c>Token</c> field.</response>
        /// <response code="400">Request body failed validation.</response>
        /// <response code="401">Email and password do not match (same response for unknown email — by design).</response>
        /// <response code="500">Unexpected server error.</response>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _userService.LoginUserAsync(dto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
            }
        }
    }
}
