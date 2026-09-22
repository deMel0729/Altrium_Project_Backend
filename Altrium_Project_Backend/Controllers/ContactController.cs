//Written by Shahmi//
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
    public class ContactController : ControllerBase
    {
        private readonly IContactRepository _contactRepository;
        private readonly ICompanyRepository _companyRepository;

        // A contact has no owner of its own, so the company it belongs to decides who
        // may touch it - which means this controller has to look companies up too.
        public ContactController(IContactRepository contactRepository, ICompanyRepository companyRepository)
        {
            _contactRepository = contactRepository;
            _companyRepository = companyRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Contact>>> GetAll()
        {
            var contacts = await _contactRepository.GetAllAsync(User.OwnerFilter());
            return contacts is null ? NotFound() : Ok(contacts);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Contact>> GetById(int id)
        {
            var item = await _contactRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Contact>> Create(Contact input)
        {
            if (!await CanUseCompany(input.CompanyId))
                return BadRequest("That company does not exist or is not yours.");

            // Records are always created live; is_active is owned by the Delete endpoint.
            input.IsActive = true;

            input.Id = await _contactRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Contact input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var existing = await _contactRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            // Stops a contact being moved into a company the caller cannot see.
            if (!await CanUseCompany(input.CompanyId))
                return BadRequest("That company does not exist or is not yours.");

            // Never take is_active from the body: an omitted value would archive the row.
            input.IsActive = existing.IsActive;

            if (!await _contactRepository.UpdateAsync(input)) return NotFound();
            var updatedContact = await _contactRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedContact is null ? NotFound() : Ok(updatedContact);
        }

        [Authorize(Roles = Roles.ManagerOrLeadership)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _contactRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _contactRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }

        // Managers pass for any company; a rep only for the companies they own.
        private async Task<bool> CanUseCompany(int companyId) =>
            await _companyRepository.GetByIdAsync(companyId, User.OwnerFilter()) is not null;
    }
}
