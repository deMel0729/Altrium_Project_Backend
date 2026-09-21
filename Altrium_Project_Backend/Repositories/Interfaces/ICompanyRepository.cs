//Written by Shahmi//
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface ICompanyRepository 
    {
        // ownerId: null = every row (manager / leadership), a user id = that user's rows only.
        Task<List<Company>> GetAllAsync(int? ownerId);
        Task<Company?> GetByIdAsync(int id, int? ownerId);
        Task<int> CreateAsync(Company c);
        Task<bool> UpdateAsync(Company c);
        Task<bool> DeleteAsync(int id);

    }
}
