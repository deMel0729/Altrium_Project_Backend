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
    public class FollowUpController : ControllerBase
    {
        private readonly IFollowUpRepository _followUpRepository;
        private readonly IDealRepository _dealRepository;

        // A follow-up hangs off a deal, so the deal is read here to check the
        // caller owns it and to take the company and lead from it.
        public FollowUpController(IFollowUpRepository followUpRepository, IDealRepository dealRepository)
        {
            _followUpRepository = followUpRepository;
            _dealRepository = dealRepository;
        }

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
            var problem = await ResolveDealAsync(input);
            if (problem is not null) return BadRequest(problem);

            // A reminder belongs to the person who set it.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            // Records are always created live. is_active is a soft-delete flag the
            // API owns - the Delete endpoint sets it, nothing else.
            input.IsActive = true;

            input.Id = await _followUpRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, FollowUp input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var existing = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            var problem = await ResolveDealAsync(input);
            if (problem is not null) return BadRequest(problem);

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            // Never take is_active from the body: a request that omits it would
            // deserialise to false and silently archive the record.
            input.IsActive = existing.IsActive;

            if (!await _followUpRepository.UpdateAsync(input)) return NotFound();
            var updatedFollowUp = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedFollowUp is null ? NotFound() : Ok(updatedFollowUp);
        }

        // A follow-up is the next action on a deal, so the deal decides three things:
        // whether the caller may attach one at all, which company and lead it
        // belongs to, and whether the work is still alive.
        private async Task<string?> ResolveDealAsync(FollowUp input)
        {
            if (input.DealId <= 0) return "Choose the deal this follow-up is about.";

            var deal = await _dealRepository.GetByIdAsync(input.DealId, User.OwnerFilter());
            if (deal is null) return "That deal does not exist or is not yours.";

            // Nothing left to chase on a lost deal. Reopen it by moving it back to
            // an open stage if the work restarts.
            if (string.Equals(deal.Stage, "Lost", StringComparison.OrdinalIgnoreCase))
                return "That deal is marked Lost, so there is nothing to follow up. Move it back to an open stage first.";

            input.CompanyId = deal.CompanyId;
            input.LeadId = deal.LeadId;
            return null;
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _followUpRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _followUpRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
