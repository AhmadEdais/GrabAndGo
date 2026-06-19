namespace GrabAndGo.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CartsController : ControllerBase
{
    private readonly ICartService cartService;
    public CartsController(ICartService cartService)
    {
        this.cartService = cartService;
    }
    [HttpGet("active")]
    [ProducesResponseType(typeof(CartSignalRDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> GetCartItems()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (claim == null || !int.TryParse(claim, out int userId))
            return Unauthorized();

        CartSignalRDto? cart = await cartService.GetActiveCartByUserIdAsync(userId);

        if (cart == null)
            return NotFound();

        return Ok(cart);
    }
}
