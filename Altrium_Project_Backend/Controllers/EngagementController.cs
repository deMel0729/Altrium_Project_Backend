//written by dew
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EngagementController : ControllerBase
    {
        private readonly IEngagementRepository _engagementRepository;
        public EngagementController(IEngagementRepository engagementRepository) => _engagementRepository = engagementRepository;

        [HttpGet]
        public async Task<ActionResult<List<Engagement>>> GetAll()
        {
            var engagements = await _engagementRepository.GetAllAsync(User.OwnerFilter());
            return engagements is null ? NotFound() : Ok(engagements);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Engagement>> GetById(int id)
        {
            var item = await _engagementRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Engagement>> Create(Engagement input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            // An activity is logged by whoever is signed in.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            input.Id = await _engagementRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Engagement input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            var existing = await _engagementRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            if (!await _engagementRepository.UpdateAsync(input)) return NotFound();
            var updatedEngagement = await _engagementRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedEngagement is null ? NotFound() : Ok(updatedEngagement);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _engagementRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _engagementRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraint on dbo.Engagement so a bad value is a 400, not a 500.
        private static string? Validate(Engagement input)
        {
            if (!CrmEnums.EngagementTypes.Contains(input.EngagementType))
                return $"EngagementType must be one of: {string.Join(", ", CrmEnums.EngagementTypes)}.";
            return null;
        }
    }
}
