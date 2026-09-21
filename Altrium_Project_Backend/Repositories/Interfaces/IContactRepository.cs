//Written by Shahmi//
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IContactRepository
    {
        // ownerId: null = every row (manager / leadership), a user id = that user's rows only.
        Task<List<Contact>> GetAllAsync(int? ownerId);
        Task<Contact?> GetByIdAsync(int id, int? ownerId);
        Task<int> CreateAsync(Contact c);
        Task<bool> UpdateAsync(Contact c);
        Task<bool> DeleteAsync(int id);

    }
}
