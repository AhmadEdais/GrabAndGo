namespace GrabAndGo.Models.Responses.Vision_System
{
    public class BindTrackResponseDto
    {
        public int BindingId { get; set; }
        public string SessionId { get; set; }
        public string TrackId { get; set; }
        public DateTime BoundAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
