// written by malan
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Models.Auth;
using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]                       // the whole API is closed by default; the
                                      // anonymous endpoints below opt out explicitly
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _tokens;
        private readonly ILogger<AuthController> _log;

        public AuthController(IUserRepository users, IPasswordHasher hasher, IJwtTokenService tokens, ILogger<AuthController> log)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
            _log = log;
        }

        // POST /api/auth/login
        // The one endpoint that has to be reachable without a token.
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            var user = await _users.GetByEmailWithHashAsync(request.Email.Trim());

            // Verify even when the user was not found, so a missing account and a wrong
            // password take about the same time and cannot be told apart by timing.
            var passwordOk = _hasher.Verify(request.Password, user?.PasswordHash);

            if (user is null || !passwordOk || !user.IsActive)
            {
                _log.LogInformation("Failed login for {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            return Ok(BuildResponse(user));
        }

        // GET /api/auth/me - lets the client rebuild its session from the token alone.
        [HttpGet("me")]
        public async Task<ActionResult<CurrentUser>> Me()
        {
            var user = await _users.GetByIdAsync(User.CallerId());
            return user is null ? Unauthorized() : Ok(ToCurrentUser(user));
        }

        // POST /api/auth/register - creating accounts is an administrative act.
        [Authorize(Roles = Roles.Leadership)]
        [HttpPost("register")]
        public async Task<ActionResult<CurrentUser>> Register(RegisterRequest request)
        {
            var problem = ValidateRole(request.UserRole);
            if (problem is not null) return BadRequest(problem);

            var email = request.Email.Trim();
            if (await _users.EmailExistsAsync(email))
                return Conflict(new { message = "That email address is already registered." });

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                UserRole = request.UserRole,
                IsActive = true,
                PasswordHash = _hasher.Hash(request.Password),
            };

            user.Id = await _users.CreateAsync(user);
            return CreatedAtAction(nameof(Me), ToCurrentUser(user));
        }

        // POST /api/auth/change-password - a user changing their own password.
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var me = await _users.GetByIdAsync(User.CallerId());
            if (me is null) return Unauthorized();

            // Re-read with the hash: the current password has to be proved again, so a
            // stolen, still-valid token cannot be used to take the account over.
            var withHash = await _users.GetByEmailWithHashAsync(me.Email);
            if (withHash is null || !_hasher.Verify(request.CurrentPassword, withHash.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect." });

            await _users.UpdatePasswordHashAsync(me.Id, _hasher.Hash(request.NewPassword));
            return NoContent();
        }

        // POST /api/auth/users/5/reset-password
        // Stands in for "forgot password" until email delivery exists: leadership sets a
        // new password and passes it to the user out of band.
        [Authorize(Roles = Roles.Leadership)]
        [HttpPost("users/{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
        {
            var user = await _users.GetByIdAsync(id);
            if (user is null) return NotFound();

            await _users.UpdatePasswordHashAsync(id, _hasher.Hash(request.NewPassword));
            _log.LogWarning("Password for user {UserId} was reset by user {ActorId}", id, User.CallerId());
            return NoContent();
        }

        // POST /api/auth/bootstrap
        // First-run only: creates the first LEADERSHIP account while no account in the
        // database has a usable password yet. It stops working the moment one does, so
        // it cannot be used to add an administrator to a live system.
        [AllowAnonymous]
        [HttpPost("bootstrap")]
        public async Task<ActionResult<CurrentUser>> Bootstrap(RegisterRequest request)
        {
            if (await _users.AnyUsablePasswordAsync())
                return Conflict(new { message = "Bootstrap is closed: an account with a password already exists." });

            var email = request.Email.Trim();
            var existing = await _users.GetByEmailWithHashAsync(email);

            if (existing is not null)
            {
                // Give an existing seeded row a real password and promote it.
                existing.UserRole = Roles.Leadership;
                existing.IsActive = true;
                existing.PasswordHash = null;             // UpdateAsync keeps the stored hash
                await _users.UpdateAsync(existing);
                await _users.UpdatePasswordHashAsync(existing.Id, _hasher.Hash(request.Password));
                _log.LogWarning("Bootstrap promoted existing user {UserId} to LEADERSHIP", existing.Id);
                return Ok(ToCurrentUser(existing));
            }

            var user = new User
            {
                Name = request.Name.Trim(),
                Email = email,
                UserRole = Roles.Leadership,
                IsActive = true,
                PasswordHash = _hasher.Hash(request.Password),
            };
            user.Id = await _users.CreateAsync(user);
            _log.LogWarning("Bootstrap created the first LEADERSHIP user {UserId}", user.Id);
            return Ok(ToCurrentUser(user));
        }

        private AuthResponse BuildResponse(User user)
        {
            var (token, expires) = _tokens.CreateAccessToken(user);
            return new AuthResponse { AccessToken = token, ExpiresAt = expires, User = ToCurrentUser(user) };
        }

        private static CurrentUser ToCurrentUser(User u) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            UserRole = u.UserRole,
            SeesEverything = Roles.SeesEverything(u.UserRole),
        };

        private static string? ValidateRole(string role) =>
            CrmEnums.UserRoles.Contains(role)
                ? null
                : $"UserRole must be one of: {string.Join(", ", CrmEnums.UserRoles)}.";
    }
}
