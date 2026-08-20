using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CompanyRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string Cols = "company_id, company_name, industry, website_link, phone_num, addressd, email, user_id, is_active, created_at";

        public async Task<List<Company>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.Company WHERE is_active = 1 ORDER BY company_id;";
            var list = new List<Company>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r)); 
            return list;

           
        }

        public async Task<Company?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.Company WHERE company_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(Company c)
        {
            const string sql = @"
            INSERT INTO dbo.Company (company_name, industry, website_link, phone_num, addressd, email, user_id, is_active, created_at)
            OUTPUT INSERTED.company_id
            VALUES (@name, @industry, @website, @phone, @address, @email, @owner_id, @is_active, @created_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, c);
            cmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(Company c)
        {
            const string sql = @"
            UPDATE dbo.Company
            SET company_name=@name, industry=@industry, website_link=@website, phone_num=@phone,
                addressd=@address, email=@email, user_id=@owner_id, is_active=@is_active
            WHERE company_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", c.Id);
            AddParams(cmd, c);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.Company SET is_active = 0 WHERE company_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, Company c)
        {
            cmd.Parameters.AddWithValue("name", c.CompanyName);
            cmd.Parameters.AddWithValue("industry", c.Industry);
            cmd.Parameters.AddWithValue("website", c.Website);
            cmd.Parameters.AddWithValue("phone", c.Phone);
            cmd.Parameters.AddWithValue("address", c.Address);
            cmd.Parameters.AddWithValue("email", c.Email);
            cmd.Parameters.AddWithValue("owner_id", c.UserId);
            cmd.Parameters.AddWithValue("is_active", c.IsActive);
        }
        private static Company Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("company_id"),
            CompanyName = r.GetStringCol("company_name"),
            Industry = r.GetStringCol("industry"),
            Website = r.GetStringCol("website_link"),
            Phone = r.GetStringCol("phone_num"),
            Address = r.GetStringCol("addressd"),
            Email = r.GetStringCol("email"),
            UserId = r.GetIntCol("user_id"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at")
        };
      



    }
}
