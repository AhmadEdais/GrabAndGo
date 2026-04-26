namespace GrabAndGo.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ICheckoutRepository _checkoutRepository;
        private readonly IGateNotificationService _gateNotificationService;

        // Inject the Repository Interface
        public CheckoutService(ICheckoutRepository checkoutRepository, IGateNotificationService gateNotificationService)
        {
            _checkoutRepository = checkoutRepository;
            _gateNotificationService = gateNotificationService;
        }

        public async Task<CheckoutVisionResponseDto> ProcessCheckoutAsync(CheckoutVisionRequestDto request)
        {
            // 1. Business Validation (Hardware Sanity Check)
            if (string.IsNullOrWhiteSpace(request.TrackId) || string.IsNullOrWhiteSpace(request.CameraCode))
            {
                return new CheckoutVisionResponseDto
                {
                    GateAction = "KeepClosed",
                    Message = "Invalid physical tracking data received.",
                    IsSuccess = false,
                    ShortfallAmount = 0
                };
            }

            // 2. Execute DB Logic
            var response = await _checkoutRepository.ProcessCheckoutAsync(request);

            // 3. Fallback for catastrophic DB nulls
            if (response == null)
            {
                return new CheckoutVisionResponseDto
                {
                    GateAction = "KeepClosed",
                    Message = "System error: Failed to process checkout.",
                    IsSuccess = false,
                    ShortfallAmount = 0
                };
            }
            if (response.SessionId.HasValue)
            {
                await _gateNotificationService.SendCheckoutStatusAsync(response);
            }

            return response;
        }
    }
}