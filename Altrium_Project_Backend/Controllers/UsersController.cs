// written by malan
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Models.Auth;
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        public UsersController(IUserRepository userRepository) => _userRepository = userRepository;

        // The team list is management information: a rep only ever sees their own
        // records, so it tells them nothing they need and names colleagues they
        // have no reason to enumerate.
        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            return users is null ? NotFound() : Ok(users);
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var item = await _userRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // Creating, changing and deactivating accounts is administration: leadership
        // only. Without this, any signed-in rep could create themselves a LEADERSHIP
        // account and own the whole system.
        [Authorize(Roles = Roles.Leadership)]
        [HttpPost]
        public async Task<ActionResult<User>> Create(UserWriteRequest input)
        {
            var invalid = Validate(input.UserRole);
            if (invalid is not null) return BadRequest(invalid);

            var email = input.Email.Trim();
            if (await _userRepository.EmailExistsAsync(email))
                return Conflict("That email address is already registered.");

            var user = new User
            {
                Name = input.Name.Trim(),
                Email = email,
                UserRole = input.UserRole,
                IsActive = true,        // Delete deactivates; nothing else sets this
                // No password is set here. The account cannot sign in until leadership
                // gives it one through POST /api/auth/users/{id}/reset-password, or it
                // is created directly through POST /api/auth/register.
                PasswordHash = "NO-LOGIN",
            };

            user.Id = await _userRepository.CreateAsync(user);
            user.PasswordHash = null;   // never echo the hash back
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [Authorize(Roles = Roles.Leadership)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, UserWriteRequest input)
        {
            var invalid = Validate(input.UserRole);
            if (invalid is not null) return BadRequest(invalid);

            var existing = await _userRepository.GetByIdAsync(id);
            if (existing is null) return NotFound();

            var email = input.Email.Trim();
            if (await _userRepository.EmailExistsAsync(email, id))
                return Conflict("That email address is already registered.");

            // Binding to a DTO instead of User is the point: a request cannot carry a
            // password hash, and role changes stay an administrative act.
            existing.Name = input.Name.Trim();
            existing.Email = email;
            existing.UserRole = input.UserRole;
            // is_active is left alone: deactivating is done through Delete, and
            // reactivating through the archive screen.
            existing.PasswordHash = null;      // UpdateAsync keeps the stored hash

            if (!await _userRepository.UpdateAsync(existing)) return NotFound();
            var updatedUser = await _userRepository.GetByIdAsync(id);

            return updatedUser is null ? NotFound() : Ok(updatedUser);
        }

        [Authorize(Roles = Roles.Leadership)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Deactivating yourself would lock the last administrator out.
            if (id == User.CallerId()) return BadRequest("You cannot deactivate your own account.");

            return await _userRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraint on dbo.[User] so a bad value is a 400, not a 500.
        private static string? Validate(string userRole)
        {
            if (!CrmEnums.UserRoles.Contains(userRole))
                return $"UserRole must be one of: {string.Join(", ", CrmEnums.UserRoles)}.";
            return null;
        }
    }
}
