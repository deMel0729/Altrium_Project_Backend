// written by malan
using System.Text.Json.Serialization;

namespace Altrium_Project_Backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Write-only: accepted on create/update but never serialised back to the client.
        // Leave it empty on an update to keep the stored hash unchanged.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? PasswordHash { get; set; }

        public string UserRole { get; set; } = string.Empty;   // CrmEnums.UserRoles
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
