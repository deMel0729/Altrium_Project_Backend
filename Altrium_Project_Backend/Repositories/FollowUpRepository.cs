using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class FollowUpRepository : IFollowUpRepository
    {
        private readonly IDbConnectionFactory _factory;
        public FollowUpRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string Cols = "follow_up_id, user_id, deal_id, company_id, lead_id, due_date, note, completed, is_active, created_at";

        public async Task<List<FollowUp>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.follow_ups WHERE is_active = 1 ORDER BY follow_up_id;";
            var list = new List<FollowUp>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<FollowUp?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.follow_ups WHERE follow_up_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(FollowUp f)
        {
            const string sql = @"
            INSERT INTO dbo.follow_ups (user_id, deal_id, company_id, lead_id, due_date, note, completed, is_active, created_at)
            OUTPUT INSERTED.follow_up_id
            VALUES (@user_id, @deal_id, @company_id, @lead_id, @due_date, @note, @completed, @is_active, @created_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, f);
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(FollowUp f)
        {
            const string sql = @"
            UPDATE dbo.follow_ups
            SET user_id=@user_id, deal_id=@deal_id, company_id=@company_id, lead_id=@lead_id,
                due_date=@due_date, note=@note, completed=@completed, is_active=@is_active
            WHERE follow_up_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", f.Id);
            AddParams(cmd, f);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.follow_ups SET is_active = 0 WHERE follow_up_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, FollowUp f)
        {
            cmd.Parameters.AddWithValue("user_id", f.UserId);
            cmd.Parameters.AddWithValue("deal_id", f.DealId);
            cmd.Parameters.AddWithValue("company_id", f.CompanyId);
            cmd.Parameters.AddWithValue("lead_id", f.LeadId);
            cmd.Parameters.AddWithValue("due_date", f.DueDate);
            cmd.Parameters.AddWithValue("note", f.Note);
            cmd.Parameters.AddWithValue("completed", f.Completed);
            cmd.Parameters.AddWithValue("is_active", f.IsActive);
        }

        private static FollowUp Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("follow_up_id"),
            UserId = r.GetIntCol("user_id"),
            DealId = r.GetIntCol("deal_id"),
            CompanyId = r.GetIntCol("company_id"),
            LeadId = r.GetIntCol("lead_id"),
            DueDate = r.GetDateTimeCol("due_date"),
            Note = r.GetStringCol("note"),
            Completed = r.GetBoolCol("completed"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at")
        };
    }
}
