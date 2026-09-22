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
    public class DealsController : ControllerBase
    {
        private readonly IDealRepository _dealRepository;
        private readonly ILeadRepository _leadRepository;
        private readonly IUserRepository _userRepository;

        // Leads: a rep may only raise a deal by converting one assigned to them.
        // Users: to check the role of whoever a deal is being assigned to.
        public DealsController(
            IDealRepository dealRepository,
            ILeadRepository leadRepository,
            IUserRepository userRepository)
        {
            _dealRepository = dealRepository;
            _leadRepository = leadRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Deal>>> GetAll()
        {
            // A rep gets their own pipeline, a manager gets all of it. The decision is
            // made from the token and applied inside the SQL query.
            var deals = await _dealRepository.GetAllAsync(User.OwnerFilter());
            return deals is null ? NotFound() : Ok(deals);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Deal>> GetById(int id)
        {
            // 404 rather than 403 for someone else's deal: it refuses without
            // confirming that the record exists.
            var item = await _dealRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Deal>> Create(Deal input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            // The owner comes from the token. A rep can only create their own deals;
            // a manager may assign one to a rep.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            var assignee = await Assignment.ValidateAsync(User, _userRepository, input.UserId, "Deals");
            if (assignee is not null) return BadRequest(assignee);

            // A rep raises a deal only by converting a lead that is already theirs,
            // and the deal inherits that lead's company. Without this a rep could
            // post a deal against any company id and, because account visibility
            // follows assignment, hand themselves that whole account.
            if (!User.SeesEverything())
            {
                var lead = await _leadRepository.GetByIdAsync(input.LeadId, User.CallerId());
                if (lead is null)
                    return BadRequest("A deal has to be converted from a lead assigned to you.");

                input.CompanyId = lead.CompanyId;
            }

            // Records are always created live. is_active is a soft-delete flag the
            // API owns - the Delete endpoint sets it, nothing else.
            input.IsActive = true;

            input.Id = await _dealRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Deal input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            // Load it under the caller's scope first: if they cannot see it, they
            // cannot change it either.
            var existing = await _dealRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            // A rep cannot hand their deal to someone else; a manager can reassign it.
            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            var assignee = await Assignment.ValidateAsync(User, _userRepository, input.UserId, "Deals");
            if (assignee is not null) return BadRequest(assignee);

            // Never take is_active from the body: a request that omits it would
            // deserialise to false and silently archive the record.
            input.IsActive = existing.IsActive;

            if (!await _dealRepository.UpdateAsync(input)) return NotFound();
            var updatedDeal = await _dealRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedDeal is null ? NotFound() : Ok(updatedDeal);
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _dealRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _dealRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraints on dbo.Deals so a bad value is a 400, not a 500.
        private static string? Validate(Deal input)
        {
            if (!CrmEnums.DealStages.Contains(input.Stage))
                return $"Stage must be one of: {string.Join(", ", CrmEnums.DealStages)}.";
            if (input.DealValue < 0)
                return "DealValue cannot be negative.";
            return null;
        }
    }
}
