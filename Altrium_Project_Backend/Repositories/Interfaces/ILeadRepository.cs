using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface ILeadRepository
    {
        Task<List<Lead>> GetAllAsync();
        Task<Lead?> GetByIdAsync(int id);
        Task<int> CreateAsync(Lead l);
        Task<bool> UpdateAsync(Lead l);
        Task<bool> DeleteAsync(int id);

    }
}
