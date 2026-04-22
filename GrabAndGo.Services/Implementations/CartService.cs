using GrabAndGo.Application.DTOs.Vision;
using GrabAndGo.Application.Interfaces;
using GrabAndGo.Services.Interfaces;

namespace GrabAndGo.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly ICartNotificationService _notificationService; // NEW

        public CartService(ICartRepository cartRepository, ICartNotificationService notificationService)
        {
            _cartRepository = cartRepository;
            _notificationService = notificationService;
        }

        public async Task<CartSignalRDto> ProcessVisionEventAsync(VisionEventRequestDto visionEvent)
        {
            var cartDto = await _cartRepository.ProcessVisionEventAsync(visionEvent);

            if (cartDto != null)
            {
                // The service simply announces the update. It doesn't care HOW it gets delivered.
                await _notificationService.BroadcastCartUpdateAsync(cartDto);
            }

            return cartDto;
        }
    }
}