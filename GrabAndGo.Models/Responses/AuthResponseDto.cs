namespace GrabAndGo.Models.Responses
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        // This will hold the JWT when they log in. 
        // During registration, you can just leave it null or empty.
        public string? Token { get; set; }
    }
}