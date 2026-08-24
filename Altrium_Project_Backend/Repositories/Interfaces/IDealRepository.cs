//written by dew
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IDealRepository
    {
        Task<List<Deal>> GetAllAsync();
        Task<Deal?> GetByIdAsync(int id);
        Task<int> CreateAsync(Deal d);
        Task<bool> UpdateAsync(Deal d);
        Task<bool> DeleteAsync(int id);

    }
}
