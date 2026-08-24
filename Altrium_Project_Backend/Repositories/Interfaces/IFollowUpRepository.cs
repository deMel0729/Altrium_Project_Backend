// written by malan
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IFollowUpRepository
    {
        Task<List<FollowUp>> GetAllAsync();
        Task<FollowUp?> GetByIdAsync(int id);
        Task<int> CreateAsync(FollowUp f);
        Task<bool> UpdateAsync(FollowUp f);
        Task<bool> DeleteAsync(int id);

    }
}
