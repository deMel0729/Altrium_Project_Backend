// written by malan
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IFollowUpRepository
    {
        // ownerId: null = every row (manager / leadership), a user id = that user's rows only.
        Task<List<FollowUp>> GetAllAsync(int? ownerId);
        Task<FollowUp?> GetByIdAsync(int id, int? ownerId);
        Task<int> CreateAsync(FollowUp f);
        Task<bool> UpdateAsync(FollowUp f);
        Task<bool> DeleteAsync(int id);

    }
}
