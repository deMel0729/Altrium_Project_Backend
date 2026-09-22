// written by malan
namespace Altrium_Project_Backend.Models
{
    // One row in the "Recently deleted" list. Deliberately thin: enough to
    // recognise the record and put it back, not a full copy of it.
    public class ArchivedItem
    {
        public string Entity { get; set; } = string.Empty;   // ArchivableEntity.Key
        public string Label { get; set; } = string.Empty;    // "Deal", "Company", ...
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerName { get; set; }

        // Null for rows archived before deleted_at existed - shown as "unknown".
        public DateTime? DeletedAt { get; set; }
    }
}
