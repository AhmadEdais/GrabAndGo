using System.ComponentModel.DataAnnotations;

namespace GrabAndGo.Models.Requests.Vision_System
{
    public class BindTrackRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; }
        [Required]
        [MaxLength(50)]
        public string TrackId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Source { get; set; }
    }
}