using System.ComponentModel.DataAnnotations;

namespace GrabAndGo.Models.DTOs
{
    public class GenerateQrRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "A valid Store ID is required.")]
        public int StoreId { get; set; }
    }
}