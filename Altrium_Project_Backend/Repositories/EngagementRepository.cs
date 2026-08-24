using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class EngagementRepository : IEngagementRepository
    {
        private readonly IDbConnectionFactory _factory;
        public EngagementRepository(IDbConnectionFactory factory) => _factory = factory;

        // NOTE: the primary key column is spelled "enagagement_id" in the database.
        private const string Cols = "enagagement_id, user_id, company_id, deal_id, engagement_name, engagement_type, engagement_description, is_active, created_at";

        public async Task<List<Engagement>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.Engagement WHERE is_active = 1 ORDER BY enagagement_id;";
            var list = new List<Engagement>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<Engagement?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.Engagement WHERE enagagement_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(Engagement e)
        {
            const string sql = @"
            INSERT INTO dbo.Engagement (user_id, company_id, deal_id, engagement_name, engagement_type, engagement_description, is_active, created_at)
            OUTPUT INSERTED.enagagement_id
            VALUES (@user_id, @company_id, @deal_id, @name, @type, @description, @is_active, @created_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, e);
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Engagement e)
        {
            const string sql = @"
            UPDATE dbo.Engagement
            SET user_id=@user_id, company_id=@company_id, deal_id=@deal_id, engagement_name=@name,
                engagement_type=@type, engagement_description=@description, is_active=@is_active
            WHERE enagagement_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", e.Id);
            AddParams(cmd, e);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.Engagement SET is_active = 0 WHERE enagagement_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, Engagement e)
        {
            cmd.Parameters.AddWithValue("user_id", e.UserId);
            cmd.Parameters.AddWithValue("company_id", e.CompanyId);
            cmd.Parameters.AddWithValue("deal_id", e.DealId);
            cmd.Parameters.AddWithValue("name", e.EngagementName);
            cmd.Parameters.AddWithValue("type", e.EngagementType);
            cmd.Parameters.AddWithValue("description", e.EngagementDescription);
            cmd.Parameters.AddWithValue("is_active", e.IsActive);
        }

        private static Engagement Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("enagagement_id"),
            UserId = r.GetIntCol("user_id"),
            CompanyId = r.GetIntCol("company_id"),
            DealId = r.GetIntCol("deal_id"),
            EngagementName = r.GetStringCol("engagement_name"),
            EngagementType = r.GetStringCol("engagement_type"),
            EngagementDescription = r.GetStringCol("engagement_description"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at")
        };
    }
}
