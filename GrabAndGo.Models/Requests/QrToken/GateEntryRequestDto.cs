using System.ComponentModel.DataAnnotations;

namespace GrabAndGo.Models.DTOs
{
    public class GateEntryRequestDto
    {
        [Required]
        public string QrCodeData { get; set; } = string.Empty;
        // Example: "4|6:1:0d03b397acdb4a92ba335834c3a024f7"
    }
}