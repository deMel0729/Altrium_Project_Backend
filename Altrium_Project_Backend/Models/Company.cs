using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Altrium_Project_Backend.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int UserId { get; set; }       // FK -> User.user_id (the owning rep)
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

    }
}
