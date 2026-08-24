// written by malan
using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class LeadRepository : ILeadRepository
    {
        private readonly IDbConnectionFactory _factory;
        public LeadRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string Cols = "lead_id, company_id, contact_id, user_id, lead_name, source, status, score, is_active, created_at, updated_at";

        public async Task<List<Lead>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.Leads WHERE is_active = 1 ORDER BY lead_id;";
            var list = new List<Lead>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<Lead?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.Leads WHERE lead_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(Lead l)
        {
            const string sql = @"
            INSERT INTO dbo.Leads (company_id, contact_id, user_id, lead_name, source, status, score, is_active, created_at, updated_at)
            OUTPUT INSERTED.lead_id
            VALUES (@company_id, @contact_id, @user_id, @name, @source, @status, @score, @is_active, @created_at, @updated_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, l);
            var now = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("created_at", now);
            cmd.Parameters.AddWithValue("updated_at", now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Lead l)
        {
            const string sql = @"
            UPDATE dbo.Leads
            SET company_id=@company_id, contact_id=@contact_id, user_id=@user_id, lead_name=@name,
                source=@source, status=@status, score=@score, is_active=@is_active, updated_at=@updated_at
            WHERE lead_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", l.Id);
            AddParams(cmd, l);
            cmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.Leads SET is_active = 0, updated_at = @updated_at WHERE lead_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, Lead l)
        {
            cmd.Parameters.AddWithValue("company_id", l.CompanyId);
            cmd.Parameters.AddWithValue("contact_id", DbHelpers.Nullable(l.ContactId));
            cmd.Parameters.AddWithValue("user_id", l.UserId);
            cmd.Parameters.AddWithValue("name", l.LeadName);
            cmd.Parameters.AddWithValue("source", l.Source);
            cmd.Parameters.AddWithValue("status", l.Status);
            cmd.Parameters.AddWithValue("score", l.Score);
            cmd.Parameters.AddWithValue("is_active", l.IsActive);
        }

        private static Lead Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("lead_id"),
            CompanyId = r.GetIntCol("company_id"),
            ContactId = r.GetNullableInt("contact_id"),
            UserId = r.GetIntCol("user_id"),
            LeadName = r.GetStringCol("lead_name"),
            Source = r.GetStringCol("source"),
            Status = r.GetStringCol("status"),
            Score = r.GetIntCol("score"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at"),
            UpdatedAt = r.GetDateTimeCol("updated_at")
        };
    }
}
