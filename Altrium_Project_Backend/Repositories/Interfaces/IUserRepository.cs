// written by malan
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<int> CreateAsync(User u);
        Task<bool> UpdateAsync(User u);
        Task<bool> DeleteAsync(int id);

    }
}
