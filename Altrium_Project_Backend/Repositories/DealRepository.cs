using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class DealRepository : IDealRepository
    {
        private readonly IDbConnectionFactory _factory;
        public DealRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string Cols = "deal_id, company_id, contact_id, user_id, lead_id, deal_name, deal_value, stage, expected_close_date, is_active, created_at";

        public async Task<List<Deal>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.Deals WHERE is_active = 1 ORDER BY deal_id;";
            var list = new List<Deal>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<Deal?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.Deals WHERE deal_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(Deal d)
        {
            const string sql = @"
            INSERT INTO dbo.Deals (company_id, contact_id, user_id, lead_id, deal_name, deal_value, stage, expected_close_date, is_active, created_at)
            OUTPUT INSERTED.deal_id
            VALUES (@company_id, @contact_id, @user_id, @lead_id, @name, @value, @stage, @expected_close_date, @is_active, @created_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, d);
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Deal d)
        {
            const string sql = @"
            UPDATE dbo.Deals
            SET company_id=@company_id, contact_id=@contact_id, user_id=@user_id, lead_id=@lead_id,
                deal_name=@name, deal_value=@value, stage=@stage, expected_close_date=@expected_close_date,
                is_active=@is_active
            WHERE deal_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", d.Id);
            AddParams(cmd, d);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.Deals SET is_active = 0 WHERE deal_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, Deal d)
        {
            cmd.Parameters.AddWithValue("company_id", d.CompanyId);
            cmd.Parameters.AddWithValue("contact_id", DbHelpers.Nullable(d.ContactId));
            cmd.Parameters.AddWithValue("user_id", d.UserId);
            cmd.Parameters.AddWithValue("lead_id", d.LeadId);
            cmd.Parameters.AddWithValue("name", d.DealName);
            cmd.Parameters.AddWithValue("value", d.DealValue);
            cmd.Parameters.AddWithValue("stage", d.Stage);
            cmd.Parameters.AddWithValue("expected_close_date", d.ExpectedCloseDate);
            cmd.Parameters.AddWithValue("is_active", d.IsActive);
        }

        private static Deal Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("deal_id"),
            CompanyId = r.GetIntCol("company_id"),
            ContactId = r.GetNullableInt("contact_id"),
            UserId = r.GetIntCol("user_id"),
            LeadId = r.GetIntCol("lead_id"),
            DealName = r.GetStringCol("deal_name"),
            DealValue = r.GetIntCol("deal_value"),
            Stage = r.GetStringCol("stage"),
            ExpectedCloseDate = r.GetDateTimeCol("expected_close_date"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at")
        };
    }
}
