//written by dew
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Data;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealsController : ControllerBase
    {
        private readonly IDealRepository _dealRepository;
        public DealsController(IDealRepository dealRepository) => _dealRepository = dealRepository;

        [HttpGet]
        public async Task<ActionResult<List<Deal>>> GetAll()
        {
            var deals = await _dealRepository.GetAllAsync();
            return deals is null ? NotFound() : Ok(deals);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Deal>> GetById(int id)
        {
            var item = await _dealRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Deal>> Create(Deal input)
        {
            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            input.Id = await _dealRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Deal input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var invalid = Validate(input);
            if (invalid is not null) return BadRequest(invalid);

            if (!await _dealRepository.UpdateAsync(input)) return NotFound();
            var updatedDeal = await _dealRepository.GetByIdAsync(id);

            return updatedDeal is null ? NotFound() : Ok(updatedDeal);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
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
