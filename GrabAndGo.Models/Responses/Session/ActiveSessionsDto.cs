namespace GrabAndGo.Models.Responses.Session;

public class ActiveSessionsDto
{
    public int SessionId { get; set; }
    public int StoreId { get; set; }
    public string? TrackId { get; set; }
    public bool IsTracked { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
