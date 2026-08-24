//written by dew
namespace Altrium_Project_Backend.Models
{
    public class Deal
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }            // FK -> Company.company_id
        public int? ContactId { get; set; }           // FK -> Contact.contact_id (nullable)
        public int UserId { get; set; }               // FK -> User.user_id (the owning rep)
        public int LeadId { get; set; }               // FK -> Leads.lead_id
        public string DealName { get; set; } = string.Empty;
        public int DealValue { get; set; }            // must be >= 0
        public string Stage { get; set; } = "Prospecting";  // CrmEnums.DealStages
        public DateTime ExpectedCloseDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
