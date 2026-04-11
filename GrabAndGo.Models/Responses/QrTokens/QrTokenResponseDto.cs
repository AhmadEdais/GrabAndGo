using System;

namespace GrabAndGo.Models.DTOs
{
    public class QrTokenResponseDto
    {
        public int TokenId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string QrCodeData { get; set; } = string.Empty; // The string Flutter turns into an image
    }
}