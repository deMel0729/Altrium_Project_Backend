// written by malan
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Data;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        public UsersController(IUserRepository userRepository) => _userRepository = userRepository;

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            return users is null ? NotFound() : Ok(users);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var item = await _userRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(User input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            input.Id = await _userRepository.CreateAsync(input);
            input.PasswordHash = null;   // never echo the hash back
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, User input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            if (!await _userRepository.UpdateAsync(input)) return NotFound();
            var updatedUser = await _userRepository.GetByIdAsync(id);

            return updatedUser is null ? NotFound() : Ok(updatedUser);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _userRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraint on dbo.[User] so a bad value is a 400, not a 500.
        private static string? Validate(User input)
        {
            if (!CrmEnums.UserRoles.Contains(input.UserRole))
                return $"UserRole must be one of: {string.Join(", ", CrmEnums.UserRoles)}.";
            return null;
        }
    }
}
