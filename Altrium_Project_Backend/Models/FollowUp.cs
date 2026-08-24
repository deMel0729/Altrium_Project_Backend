namespace Altrium_Project_Backend.Models
{
    public class FollowUp
    {
        public int Id { get; set; }
        public int UserId { get; set; }               // FK -> User.user_id
        public int DealId { get; set; }               // FK -> Deals.deal_id
        public int CompanyId { get; set; }            // FK -> Company.company_id
        public int LeadId { get; set; }               // FK -> Leads.lead_id
        public DateTime DueDate { get; set; }
        public string Note { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
