using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IContactRepository
    {
        Task<List<Contact>> GetAllAsync();
        Task<Contact?> GetByIdAsync(int id);
        Task<int> CreateAsync(Contact c);
        Task<bool> UpdateAsync(Contact c);
        Task<bool> DeleteAsync(int id);

    }
}
