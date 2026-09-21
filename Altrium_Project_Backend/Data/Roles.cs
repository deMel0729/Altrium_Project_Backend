// written by malan
namespace Altrium_Project_Backend.Data
{
    // The three role strings that the CHECK constraint on dbo.[User] allows, plus the
    // one rule that decides how much of the CRM a caller can see.
    //
    //   SALES REP      - own records only (rows where user_id = their own id)
    //   SALES MANAGER  - everything
    //   LEADERSHIP     - everything, plus user administration
    public static class Roles
    {
        public const string SalesRep = "SALES REP";
        public const string SalesManager = "SALES MANAGER";
        public const string Leadership = "LEADERSHIP";

        // Comma lists for [Authorize(Roles = ...)].
        public const string ManagerOrLeadership = SalesManager + "," + Leadership;

        // A manager sees the whole pipeline; a rep sees only what they own.
        public static bool SeesEverything(string? role) =>
            string.Equals(role, SalesManager, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, Leadership, StringComparison.OrdinalIgnoreCase);
    }
}
