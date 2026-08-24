using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactRepository _contactRepository;
        public ContactController(IContactRepository contactRepository) => _contactRepository = contactRepository;

        [HttpGet]
        public async Task<ActionResult<List<Contact>>> GetAll()
        {
            var contacts = await _contactRepository.GetAllAsync();
            return contacts is null ? NotFound() : Ok(contacts);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Contact>> GetById(int id)
        {
            var item = await _contactRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Contact>> Create(Contact input)
        {
            input.Id = await _contactRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Contact input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");
            if (!await _contactRepository.UpdateAsync(input)) return NotFound();
            var updatedContact = await _contactRepository.GetByIdAsync(id);

            return updatedContact is null ? NotFound() : Ok(updatedContact);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _contactRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
