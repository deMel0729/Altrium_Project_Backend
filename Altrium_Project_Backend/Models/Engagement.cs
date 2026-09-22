//written by dew
namespace Altrium_Project_Backend.Models
{
    public class Engagement
    {
        public int Id { get; set; }
        public int UserId { get; set; }               // FK -> User.user_id
        public int CompanyId { get; set; }            // FK -> Company.company_id

        // An engagement hangs off exactly one of these. A call while qualifying a
        // lead belongs to the lead; a call about pricing belongs to the deal.
        public int? DealId { get; set; }              // FK -> Deals.deal_id (nullable)
        public int? LeadId { get; set; }              // FK -> Leads.lead_id (nullable)
        public string EngagementName { get; set; } = string.Empty;
        public string EngagementType { get; set; } = string.Empty;   // CrmEnums.EngagementTypes
        public string EngagementDescription { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
