//written by dew
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IEngagementRepository
    {
        // ownerId: null = every row (manager / leadership), a user id = that user's rows only.
        Task<List<Engagement>> GetAllAsync(int? ownerId);
        Task<Engagement?> GetByIdAsync(int id, int? ownerId);
        Task<int> CreateAsync(Engagement e);
        Task<bool> UpdateAsync(Engagement e);
        Task<bool> DeleteAsync(int id);

    }
}
