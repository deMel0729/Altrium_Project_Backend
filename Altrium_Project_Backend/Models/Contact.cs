namespace Altrium_Project_Backend.Models
{
    public class Contact
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }            // FK -> Company.company_id
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string? Phone { get; set; }            // nullable in the database
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
