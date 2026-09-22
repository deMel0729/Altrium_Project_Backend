// written by malan
namespace Altrium_Project_Backend.Data
{
    // Shared SQL fragments for row-level scoping, so the same rule cannot drift
    // between the repositories that depend on it.
    public static class Scopes
    {
        // Which companies a sales rep may see.
        //
        // Accounts are created by managers, so ownership alone is not enough: a rep
        // sees a company because work there was assigned to them. Assigning the lead
        // is what grants the access, and reassigning it takes the access away again.
        //
        // Used with an @owner parameter; managers and leadership pass NULL and skip
        // the filter entirely.
        public const string VisibleCompanyIds = @"
            SELECT company_id FROM dbo.Company WHERE user_id = @owner
            UNION SELECT company_id FROM dbo.Leads WHERE user_id = @owner AND is_active = 1
            UNION SELECT company_id FROM dbo.Deals WHERE user_id = @owner AND is_active = 1";

        // Drop-in WHERE clause for any table with a company_id column.
        public const string CompanyScope =
            " AND (@owner IS NULL OR company_id IN (" + VisibleCompanyIds + "))";
    }
}
