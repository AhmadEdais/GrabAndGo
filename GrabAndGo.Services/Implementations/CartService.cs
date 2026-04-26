namespace GrabAndGo.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly ICartNotificationService _cartNotificationService; 

        public CartService(ICartRepository cartRepository, ICartNotificationService notificationService)
        {
            _cartRepository = cartRepository;
            _cartNotificationService = notificationService;
        }

        public async Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto visionEvent)
        {
            var cartDto = await _cartRepository.ProcessVisionEventAsync(visionEvent);

            if (cartDto != null)
            {
                // The service simply announces the update. It doesn't care HOW it gets delivered.
                await _cartNotificationService.BroadcastCartUpdateAsync(cartDto);
            }

            return cartDto;
        }
    }
}