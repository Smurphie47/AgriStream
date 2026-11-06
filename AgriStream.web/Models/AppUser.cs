namespace AgriStream.web.Models
{
    public class AppUser
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Farmer";

        public DateTime CreatedAt { get; set; }
    }
}