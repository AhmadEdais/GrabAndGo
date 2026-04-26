namespace GrabAndGo.Repositories.Interfaces
{
    public interface ICheckoutRepository
    {
        Task<CheckoutVisionResponseDto?> ProcessCheckoutAsync(CheckoutVisionRequestDto request);
    }
}