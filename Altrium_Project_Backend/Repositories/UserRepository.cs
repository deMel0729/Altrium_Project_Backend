using Altrium_Project_Backend.Repositories.Interfaces;
using Altrium_Project_Backend.Models;
using Microsoft.Data.SqlClient;
using Altrium_Project_Backend.Data;

namespace Altrium_Project_Backend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _factory;
        public UserRepository(IDbConnectionFactory factory) => _factory = factory;

        // password_hash is deliberately left out of the read list so it is never returned to a caller.
        // "User" is a reserved word in T-SQL, so the table name has to stay bracketed.
        private const string Cols = "user_id, name, email, user_role, is_active, created_at, updated_at";

        public async Task<List<User>> GetAllAsync()
        {
            var sql = $"SELECT {Cols} FROM dbo.[User] WHERE is_active = 1 ORDER BY user_id;";
            var list = new List<User>();
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) list.Add(Map(r));
            return list;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            var sql = $"SELECT {Cols} FROM dbo.[User] WHERE user_id=@id AND is_active = 1;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync() ? Map(r) : null;
        }

        public async Task<int> CreateAsync(User u)
        {
            const string sql = @"
            INSERT INTO dbo.[User] (name, email, password_hash, user_role, is_active, created_at, updated_at)
            OUTPUT INSERTED.user_id
            VALUES (@name, @email, @password_hash, @user_role, @is_active, @created_at, @updated_at);";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            AddParams(cmd, u);
            var now = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("password_hash", u.PasswordHash ?? string.Empty);
            cmd.Parameters.AddWithValue("created_at", now);
            cmd.Parameters.AddWithValue("updated_at", now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        public async Task<bool> UpdateAsync(User u)
        {
            // COALESCE keeps the stored hash when the caller does not send a new one.
            const string sql = @"
            UPDATE dbo.[User]
            SET name=@name, email=@email, password_hash=COALESCE(@password_hash, password_hash),
                user_role=@user_role, is_active=@is_active, updated_at=@updated_at
            WHERE user_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", u.Id);
            AddParams(cmd, u);
            cmd.Parameters.AddWithValue("password_hash",
                DbHelpers.Nullable(string.IsNullOrWhiteSpace(u.PasswordHash) ? null : u.PasswordHash));
            cmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE dbo.[User] SET is_active = 0, updated_at = @updated_at WHERE user_id=@id;";
            await using var conn = _factory.Create();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private static void AddParams(SqlCommand cmd, User u)
        {
            cmd.Parameters.AddWithValue("name", u.Name);
            cmd.Parameters.AddWithValue("email", u.Email);
            cmd.Parameters.AddWithValue("user_role", u.UserRole);
            cmd.Parameters.AddWithValue("is_active", u.IsActive);
        }

        private static User Map(SqlDataReader r) => new()
        {
            Id = r.GetIntCol("user_id"),
            Name = r.GetStringCol("name"),
            Email = r.GetStringCol("email"),
            UserRole = r.GetStringCol("user_role"),
            IsActive = r.GetBoolCol("is_active"),
            CreatedAt = r.GetDateTimeCol("created_at"),
            UpdatedAt = r.GetNullableDateTime("updated_at")
        };
    }
}
