//wriiten by Shahmi//
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Security;
using Microsoft.AspNetCore.Authorization;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        public CompaniesController(ICompanyRepository companyRepository) => _companyRepository = companyRepository;

        [HttpGet]
        public async Task<ActionResult<List<Company>>> GetAll()
        {
            // A rep sees the accounts they own; a manager sees every account.
            var companies = await _companyRepository.GetAllAsync(User.OwnerFilter());
            return companies is null ? NotFound() : Ok(companies);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Company>> GetById(int id)
        {
            var item = await _companyRepository.GetByIdAsync(id, User.OwnerFilter());
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Company>> Create(Company input)
        {
            // Ownership is taken from the token, never from the request body.
            input.UserId = User.OwnerForNewRecord(input.UserId);

            input.Id = await _companyRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Company input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");

            var existing = await _companyRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            input.UserId = User.SeesEverything()
                ? (input.UserId > 0 ? input.UserId : existing.UserId)
                : existing.UserId;

            if (!await _companyRepository.UpdateAsync(input)) return NotFound();
            var updatedCompany = await _companyRepository.GetByIdAsync(id, User.OwnerFilter());

            return updatedCompany is null ? NotFound() : Ok(updatedCompany);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _companyRepository.GetByIdAsync(id, User.OwnerFilter());
            if (existing is null) return NotFound();

            return await _companyRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
