using System.ComponentModel.DataAnnotations;

namespace GrabAndGo.Models.Requests.Wallet
{
    public class TopUpRequestDto
    {
        [Required]
        [Range(0.01, 10000.00, ErrorMessage = "Top-up amount must be between $0.01 and $10,000.")]
        public decimal Amount { get; set; }
    }
}
