namespace GrabAndGo.Models.Responses
{
    public class CheckoutVisionResponseDto
    {
        public string GateAction { get; set; } = null!; // "OpenGate" or "KeepClosed"
        public string Message { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public decimal ShortfallAmount { get; set; }

        public int? SessionId { get; set; } 
        public int? TransactionId { get; set; }
        public decimal? TotalDeducted { get; set; }
        public decimal? RemainingBalance { get; set; }
    }
}