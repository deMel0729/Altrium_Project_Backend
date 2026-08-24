//wriiten by Shahmi//
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepository;
        public CompaniesController(ICompanyRepository companyRepository) => _companyRepository = companyRepository;

        [HttpGet]
        public async Task<ActionResult<List<Company>>> GetAll()
        {
            var companies = await _companyRepository.GetAllAsync();
            return companies is null ? NotFound() : Ok(companies);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Company>> GetById(int id)
        {
            var item = await _companyRepository.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Company>> Create(Company input)
        {        
            input.Id = await _companyRepository.CreateAsync(input);
            return CreatedAtAction(nameof(GetById), new { id = input.Id }, input);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Company input)
        {
            if (id != input.Id) return BadRequest("Route id and body id do not match.");
            if (!await _companyRepository.UpdateAsync(input)) return NotFound();
            var updatedCompany = await _companyRepository.GetByIdAsync(id);

            return updatedCompany is null ? NotFound() : Ok(updatedCompany);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await _companyRepository.DeleteAsync(id) ? NoContent() : NotFound();
        }


    }
}
