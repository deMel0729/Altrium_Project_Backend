// written by malan
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FollowUpController : ControllerBase
    {
        private readonly IFollowUpRepository _followUpRepository;
        public FollowUpController(IFollowUpRepository followUpRepository) => _followUpRepository = followUpRepository;

        [HttpGet]
        public async Task<ActionResult<List<FollowUp>>> GetAll()
        {
            // For a rep this is exactly the "My follow-ups" list: the scope is the
            // caller, so no client-side filtering is needed or trusted.
            var followUps = await _followUpRepository.GetAllAsync(User.OwnerFilter());
            return followUps is null ? NotFound() : Ok(followUps);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FollowUp>> GetById(int id)
        {
            var item = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<FollowUp>> Create(FollowUp input)
        {
            // A reminder belongs to the person who set it.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            input.Id = await _followUpRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, FollowUp input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var existing = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            if (!await _followUpRepository.UpdateAsync(input)) return NotFound();
            var updatedFollowUp = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedFollowUp is null ? NotFound() : Ok(updatedFollowUp);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _followUpRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
