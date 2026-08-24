using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IEngagementRepository
    {
        Task<List<Engagement>> GetAllAsync();
        Task<Engagement?> GetByIdAsync(int id);
        Task<int> CreateAsync(Engagement e);
        Task<bool> UpdateAsync(Engagement e);
        Task<bool> DeleteAsync(int id);

    }
}
