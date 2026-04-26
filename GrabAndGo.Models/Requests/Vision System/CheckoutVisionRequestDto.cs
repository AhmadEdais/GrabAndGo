namespace GrabAndGo.Models.Requests
{
    public class CheckoutVisionRequestDto
    {
        public string TrackId { get; set; } = null!;
        public string CameraCode { get; set; } = null!;
        public DateTime EventTime { get; set; }
    }
}