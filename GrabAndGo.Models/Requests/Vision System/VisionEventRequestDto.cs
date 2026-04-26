namespace GrabAndGo.Application.DTOs.Vision
{
    public class VisionEventRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string TrackId { get; set; }

        [Required]
        [MaxLength(100)]
        public string AiLabel { get; set; }

        [Required]
        [RegularExpression("^(Pick|Return)$", ErrorMessage = "Action must be either 'Pick' or 'Return'.")]
        public string Action { get; set; }

        [Required]
        public DateTime EventTime { get; set; }

        [Required]
        [Range(0.0, 1.0, ErrorMessage = "Confidence score must be between 0.0 and 1.0.")]
        public decimal Confidence { get; set; }

        [Required]
        [MaxLength(50)]
        public string CameraCode { get; set; }
    }
}