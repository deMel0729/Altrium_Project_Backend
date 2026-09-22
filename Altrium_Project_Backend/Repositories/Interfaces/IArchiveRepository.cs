// written by malan
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Models;
namespace Altrium_Project_Backend.Repositories.Interfaces
{
    public interface IArchiveRepository
    {
        // Every soft-deleted row, newest deletion first.
        Task<List<ArchivedItem>> GetAllAsync();

        // Sets is_active back to 1. The entity comes from the fixed allowlist,
        // never straight from the request.
        Task<bool> RestoreAsync(ArchivableEntity entity, int id);
    }
}
