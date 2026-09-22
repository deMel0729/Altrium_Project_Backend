// written by malan
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
    public class LeadsController : ControllerBase
    {
        private readonly ILeadRepository _leadRepository;
        private readonly IUserRepository _userRepository;

        // Users are read here to check the role of whoever a lead is being
        // assigned to - a manager may not hand work upwards.
        public LeadsController(ILeadRepository leadRepository, IUserRepository userRepository)
        {
            _leadRepository = leadRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Lead>>> GetAll()
        {
            var leads = await _leadRepository.GetAllAsync(User.OwnerFilter());
            return leads is null ? NotFound() : Ok(leads);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Lead>> GetById(int id)
        {
            var item = await _leadRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        // Managers create leads and assign them to a rep. If reps could create
        // leads they could point one at any company and grant themselves access
        // to that account, because visibility follows assignment.
        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpPost]
        public async Task<ActionResult<Lead>> Create(Lead input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            // "Assign lead to a rep" is a manager's job; a rep's leads are their own.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            var assignee = await Assignment.ValidateAsync(User, _userRepository, input.UserId, "Leads");
            if (assignee is not null) return BadRequest(assignee);

            // Records are always created live. is_active is a soft-delete flag the
            // API owns - the Delete endpoint sets it, nothing else.
            input.IsActive = true;

            input.Id = await _leadRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Lead input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            var existing = await _leadRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            var assignee = await Assignment.ValidateAsync(User, _userRepository, input.UserId, "Leads");
            if (assignee is not null) return BadRequest(assignee);

            // Never take is_active from the body: a request that omits it would
            // deserialise to false and silently archive the record.
            input.IsActive = existing.IsActive;

            if (!await _leadRepository.UpdateAsync(input)) return NotFound();
            var updatedLead = await _leadRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedLead is null ? NotFound() : Ok(updatedLead);
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _leadRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _leadRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraints on dbo.Leads so a bad value is a 400, not a 500.
        private static string? Validate(Lead input)
        {
            if (!CrmEnums.LeadStatuses.Contains(input.Status))
                return $"Status must be one of: {string.Join(", ", CrmEnums.LeadStatuses)}.";
            return null;
        }
    }
}
