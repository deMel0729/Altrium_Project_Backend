using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface ICompanyRepository 
    {
        Task <List<Company>> GetAllAsync();
        Task<Company?> GetByIdAsync(int id);
        Task<int> CreateAsync(Company c);
        Task<bool> UpdateAsync(Company c);
        Task<bool> DeleteAsync(int id);

    }
}
