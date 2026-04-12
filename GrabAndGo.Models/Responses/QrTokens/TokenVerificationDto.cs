using System;

namespace GrabAndGo.Models.DTOs
{
    public class TokenVerificationDto
    {
        public int TokenId { get; set; }
        public int UserId { get; set; }
        public int StoreId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; } // Nullable because it might not be burned yet!
    }
}