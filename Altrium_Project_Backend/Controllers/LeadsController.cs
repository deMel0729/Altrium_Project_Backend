// written by malan
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Data;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly ILeadRepository _leadRepository;
        public LeadsController(ILeadRepository leadRepository) => _leadRepository = leadRepository;

        [HttpGet]
        public async Task<ActionResult<List<Lead>>> GetAll()
        {
            var leads = await _leadRepository.GetAllAsync();
            return leads is null ? NotFound() : Ok(leads);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Lead>> GetById(int id)
        {
            var item = await _leadRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Lead>> Create(Lead input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            input.Id = await _leadRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Lead input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            if (!await _leadRepository.UpdateAsync(input)) return NotFound();
            var updatedLead = await _leadRepository.GetByIdAsync(id);

            return updatedLead is null ? NotFound() : Ok(updatedLead);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _leadRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Mirrors the CHECK constraints on dbo.Leads so a bad value is a 400, not a 500.
        private static string? Validate(Lead input)
        {
            if (!CrmEnums.LeadStatuses.Contains(input.Status))
                return $"Status must be one of: {string.Join(", ", CrmEnums.LeadStatuses)}.";
            if (input.Score < 0 || input.Score > 100)
                return "Score must be between 0 and 100.";
            return null;
        }
    }
}
