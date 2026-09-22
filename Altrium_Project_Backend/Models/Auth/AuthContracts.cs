// written by malan
using System.ComponentModel.DataAnnotations;

namespace Altrium_Project_Backend.Models.Auth
{
    // Request/response shapes for the auth endpoints. They are deliberately separate
    // from the User entity: binding straight to User would let a caller post a
    // user_role or is_active of their choosing.

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string UserRole { get; set; } = string.Empty;   // must be one of CrmEnums.UserRoles
    }

    // A user maintaining their own name and email. Role and account status are
    // deliberately absent: changing those stays with leadership.
    public class UpdateProfileRequest
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // What the client is told about the signed-in user. No password material, ever.
    public class CurrentUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public bool SeesEverything { get; set; }
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public CurrentUser User { get; set; } = new();
    }

    // Used by UsersController so administration cannot be turned into a password
    // back door: passwords are only ever set through the auth endpoints.
    public class UserWriteRequest
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string UserRole { get; set; } = string.Empty;
    }
}
