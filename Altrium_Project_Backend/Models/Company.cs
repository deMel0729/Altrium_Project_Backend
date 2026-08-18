namespace Altrium_Project_Backend.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public int Phone { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserId { get; set; }       // FK -> User.user_id (the owning rep)
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

    }
}
