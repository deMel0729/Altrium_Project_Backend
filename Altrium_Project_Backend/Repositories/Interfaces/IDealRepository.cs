//written by dew
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IDealRepository
    {
        // ownerId: null = every row (manager / leadership), a user id = that user's rows only.
        Task<List<Deal>> GetAllAsync(int? ownerId);
        Task<Deal?> GetByIdAsync(int id, int? ownerId);
        Task<int> CreateAsync(Deal d);
        Task<bool> UpdateAsync(Deal d);
        Task<bool> DeleteAsync(int id);

    }
}
