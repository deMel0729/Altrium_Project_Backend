// written by malan
namespace Altrium_Project_Backend.Models
{
    public class Lead
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }            // FK -> Company.company_id
        public int? ContactId { get; set; }           // FK -> Contact.contact_id (nullable)
        public int UserId { get; set; }               // FK -> User.user_id (the owning rep)
        public string LeadName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = "New";   // CrmEnums.LeadStatuses
        public int Score { get; set; }                // 0 - 100
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
