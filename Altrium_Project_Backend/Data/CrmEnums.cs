namespace Altrium_Project_Backend.Data
{
    public static class CrmEnums
    {
        // The allowed values for each SQL Server CHECK constraint. Controllers validate
        // against these so an invalid value returns a clean 400 instead of a database error.

        public static readonly HashSet<string> UserRoles = new() {"LEADERSHIP" ,"SALE REP","SALES MANAGER"};
        public static readonly HashSet<string> LeadStatuses = new() { "New", "Contacted", "Qualified", "Lost" };
        public static readonly HashSet<string> DealStages = new() { "Prospecting", "Proposal", "Negotiation", "Won", "Lost" };
        public static readonly HashSet<string> EngagementTypes = new() { "Call", "Meeting", "Email", "Note" };


    }
}
