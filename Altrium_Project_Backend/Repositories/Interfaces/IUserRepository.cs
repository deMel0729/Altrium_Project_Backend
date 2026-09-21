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

        // --- authentication ---------------------------------------------------
        // The only read that touches password_hash. Everything else in this
        // repository deliberately leaves the column out of its column list.
        Task<User?> GetByEmailWithHashAsync(string email);
        Task<bool> EmailExistsAsync(string email, int? excludingUserId = null);
        Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash);

        // True while no account has a usable password yet - the one-time window in
        // which the bootstrap endpoint is allowed to create the first administrator.
        Task<bool> AnyUsablePasswordAsync();
    }
}
