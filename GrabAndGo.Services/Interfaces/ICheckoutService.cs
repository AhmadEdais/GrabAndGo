namespace GrabAndGo.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<CheckoutVisionResponseDto> ProcessCheckoutAsync(CheckoutVisionRequestDto request);
    }
}