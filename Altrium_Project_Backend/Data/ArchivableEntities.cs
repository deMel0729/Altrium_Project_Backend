// written by malan
namespace Altrium_Project_Backend.Data
{
    // Everything the archive screen needs to know about one soft-deletable table.
    // Table and column names are interpolated into SQL, so they may ONLY ever come
    // from the fixed list below - never from a request. Lookup is by key against
    // that list, which is what keeps this safe from injection.
    public record ArchivableEntity(
        string Key,
        string Label,
        string Table,
        string IdColumn,
        string NameColumn,
        string? OwnerColumn);

    public static class ArchivableEntities
    {
        public static readonly IReadOnlyList<ArchivableEntity> All = new List<ArchivableEntity>
        {
            new("companies",   "Company",    "dbo.Company",    "company_id",     "company_name",    "user_id"),
            new("contacts",    "Contact",    "dbo.Contact",    "contact_id",     "contact_name",    null),
            new("leads",       "Lead",       "dbo.Leads",      "lead_id",        "lead_name",       "user_id"),
            new("deals",       "Deal",       "dbo.Deals",      "deal_id",        "deal_name",       "user_id"),
            // NOTE: the primary key really is spelled "enagagement_id" in the database.
            new("engagements", "Engagement", "dbo.Engagement", "enagagement_id", "engagement_name", "user_id"),
            new("followups",   "Follow-up",  "dbo.follow_ups", "follow_up_id",   "note",            "user_id"),
            new("users",       "User",       "dbo.[User]",     "user_id",        "name",            null),
        };

        public static ArchivableEntity? Find(string? key) =>
            All.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
    }
}
