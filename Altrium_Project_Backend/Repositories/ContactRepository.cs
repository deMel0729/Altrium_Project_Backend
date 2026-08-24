//Written by Shahmi//
using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ContactRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string Cols = "contact_id, company_id, contact_name, email, position, phone_num, is_active, created_at";

        public async Task<List<Contact>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.Contact WHERE is_active = 1 ORDER BY contact_id;";
            var list = new List<Contact>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<Contact?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.Contact WHERE contact_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(Contact c)
        {
            const string sql = @"
            INSERT INTO dbo.Contact (company_id, contact_name, email, position, phone_num, is_active, created_at)
            OUTPUT INSERTED.contact_id
            VALUES (@company_id, @name, @email, @position, @phone, @is_active, @created_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, c);
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Contact c)
        {
            const string sql = @"
            UPDATE dbo.Contact
            SET company_id=@company_id, contact_name=@name, email=@email, position=@position,
                phone_num=@phone, is_active=@is_active
            WHERE contact_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", c.Id);
            AddParams(cmd, c);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.Contact SET is_active = 0 WHERE contact_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, Contact c)
        {
            cmd.Parameters.AddWithValue("company_id", c.CompanyId);
            cmd.Parameters.AddWithValue("name", c.ContactName);
            cmd.Parameters.AddWithValue("email", c.Email);
            cmd.Parameters.AddWithValue("position", c.Position);
            cmd.Parameters.AddWithValue("phone", DbHelpers.Nullable(c.Phone));
            cmd.Parameters.AddWithValue("is_active", c.IsActive);
        }

        private static Contact Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("contact_id"),
            CompanyId = r.GetIntCol("company_id"),
            ContactName = r.GetStringCol("contact_name"),
            Email = r.GetStringCol("email"),
            Position = r.GetStringCol("position"),
            Phone = r.GetNullableString("phone_num"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at")
        };
    }
}
