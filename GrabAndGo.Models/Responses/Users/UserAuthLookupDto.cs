namespace GrabAndGo.Models.Responses.Users
{
    public class UserAuthLookupDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        // We MUST pull this from the DB so BCrypt can verify the password
        public string PasswordHash { get; set; }
    }
}