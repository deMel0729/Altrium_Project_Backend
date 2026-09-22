// written by malan
using Altrium_Project_Backend.Data;
using Altrium_Project_Backend.Models;
using Altrium_Project_Backend.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace Altrium_Project_Backend.Repositories
{
    // Reads and restores soft-deleted rows across every table that has them.
    //
    // The SQL is assembled from ArchivableEntities, a fixed in-code list - no part
    // of it comes from the request, so interpolating table and column names here
    // cannot be turned into an injection. The id is still a parameter.
    public class ArchiveRepository : IArchiveRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ArchiveRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<ArchivedItem>> GetAllAsync()
        {
            // One SELECT per table, stitched together, then joined once to dbo.[User]
            // so the list can say who owned each record.
            var parts = ArchivableEntities.All.Select(e => $@"
                SELECT '{e.Key}' AS entity,
                       {e.IdColumn} AS id,
                       CAST({e.NameColumn} AS NVARCHAR(200)) AS name,
                       deleted_at,
                       {(e.OwnerColumn ?? "NULL")} AS owner_id
                FROM {e.Table}
                WHERE is_active = 0");

            var sql = $@"
                SELECT a.entity, a.id, a.name, a.deleted_at, u.name AS owner_name
                FROM ({string.Join("\n                UNION ALL\n", parts)}) AS a
                LEFT JOIN dbo.[User] u ON u.user_id = a.owner_id
                ORDER BY a.deleted_at DESC, a.id DESC;";

            var labels = ArchivableEntities.All.ToDictionary(e => e.Key, e => e.Label);
            var list = new List<ArchivedItem>();

            await using var conn = await _factory.CreateOpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var key = r.GetStringCol("entity");
                list.Add(new ArchivedItem
                {
                    Entity = key,
                    Label = labels.TryGetValue(key, out var label) ? label : key,
                    Id = r.GetIntCol("id"),
                    Name = r.GetNullableString("name") ?? string.Empty,
                    DeletedAt = r.GetNullableDateTime("deleted_at"),
                    OwnerName = r.GetNullableString("owner_name"),
                });
            }
            return list;
        }

        public async Task<bool> RestoreAsync(ArchivableEntity entity, int id)
        {
            // Only flips the flag back. Related records stay as they are: restoring a
            // company does not un-delete the contacts that were deleted separately.
            var sql = $"UPDATE {entity.Table} SET is_active = 1, deleted_at = NULL WHERE {entity.IdColumn} = @id AND is_active = 0;";
            await using var conn = await _factory.CreateOpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
