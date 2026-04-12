namespace GrabAndGo.Models.DTOs
{
    public class GateEntryResponseDto
    {
        // The ID of the active shopping trip. 
        // The Vision system will use this later to bind the camera track!
        public int SessionId { get; set; }

        // The ID of the empty cart we just created for them.
        public int CartId { get; set; }

        // A simple status message for the hardware/logs
        public string Message { get; set; } = "Access Granted";
    }
}