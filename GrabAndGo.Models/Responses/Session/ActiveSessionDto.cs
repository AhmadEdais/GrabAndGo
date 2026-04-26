namespace GrabAndGo.Models.Responses.Session
{
    public class ActiveSessionDto
    {
        public int SessionId { get; set; }
        public int StoreId { get; set; }
        public string StoreCode { get; set; } = null!;
        public string StoreName { get; set; } = null!;
        public DateTime StartedAt { get; set; }
        public int? CartId { get; set; }
        public int? CartVersion { get; set; }
        public string SessionStatus { get; set; } = null!;
    }
}
