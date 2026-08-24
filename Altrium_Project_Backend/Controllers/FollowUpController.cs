using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FollowUpController : ControllerBase
    {
        private readonly IFollowUpRepository _followUpRepository;
        public FollowUpController(IFollowUpRepository followUpRepository) => _followUpRepository = followUpRepository;

        [HttpGet]
        public async Task<ActionResult<List<FollowUp>>> GetAll()
        {
            var followUps = await _followUpRepository.GetAllAsync();
            return followUps is null ? NotFound() : Ok(followUps);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FollowUp>> GetById(int id)
        {
            var item = await _followUpRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<FollowUp>> Create(FollowUp input)
        {
            input.Id = await _followUpRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, FollowUp input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");
            if (!await _followUpRepository.UpdateAsync(input)) return NotFound();
            var updatedFollowUp = await _followUpRepository.GetByIdAsync(id);

            return updatedFollowUp is null ? NotFound() : Ok(updatedFollowUp);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _followUpRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
