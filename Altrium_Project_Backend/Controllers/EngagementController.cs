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
        private readonly ILeadRepository _leadRepository;
        private readonly IDealRepository _dealRepository;

        // Leads and deals are read here to check that the caller can actually see
        // whichever one they are attaching the activity to.
        public EngagementController(
            IEngagementRepository engagementRepository,
            ILeadRepository leadRepository,
            IDealRepository dealRepository)
        {
            _engagementRepository = engagementRepository;
            _leadRepository = leadRepository;
            _dealRepository = dealRepository;
        }

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

            var parentProblem = await ResolveParentAsync(input);
            if (parentProblem is not null) return BadRequest(parentProblem);

            // An activity is logged by whoever is signed in.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            // Records are always created live. is_active is a soft-delete flag the
            // API owns - the Delete endpoint sets it, nothing else.
            input.IsActive = true;

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

            var parentProblem = await ResolveParentAsync(input);
            if (parentProblem is not null) return BadRequest(parentProblem);

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            // Never take is_active from the body: a request that omits it would
            // deserialise to false and silently archive the record.
            input.IsActive = existing.IsActive;

            if (!await _engagementRepository.UpdateAsync(input)) return NotFound();
            var updatedEngagement = await _engagementRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedEngagement is null ? NotFound() : Ok(updatedEngagement);
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
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

        // An engagement hangs off exactly one of a lead or a deal, and the company
        // is taken from whichever it is - never from the request. That keeps the
        // activity, its parent and its account consistent, and stops a caller
        // attaching a record to something they cannot see.
        private async Task<string?> ResolveParentAsync(Engagement input)
        {
            var hasLead = input.LeadId is > 0;
            var hasDeal = input.DealId is > 0;

            if (hasLead == hasDeal)
                return "Log the activity against either a lead or a deal - one, not both.";

            if (hasLead)
            {
                var lead = await _leadRepository.GetByIdAsync(input.LeadId!.Value, User.OwnerFilter());
                if (lead is null) return "That lead does not exist or is not yours.";

                input.CompanyId = lead.CompanyId;
                input.DealId = null;
                return null;
            }

            var deal = await _dealRepository.GetByIdAsync(input.DealId!.Value, User.OwnerFilter());
            if (deal is null) return "That deal does not exist or is not yours.";

            input.CompanyId = deal.CompanyId;
            input.LeadId = null;
            return null;
        }
    }
}
