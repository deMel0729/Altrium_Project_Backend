// written by the whole team
using Microsoft.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
namespace Altrium_Project_Backend.Data

// connecting the database with the backend using the connection string from the appsettings.json file

{
    public interface IDbConnectionFactory
    {
        SqlConnection Create();

        // Opens the connection with retries on Azure SQL transient errors (e.g. 40613
        // "database not currently available" while a serverless DB resumes from pause).
        Task<SqlConnection> CreateOpenAsync();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        // Azure SQL transient error numbers worth retrying.
        // 40613: database not currently available (e.g. resuming from auto-pause)
        // 40197/40501/49918: service busy/throttled
        // 4060: cannot open database (can occur transiently on failover)
        // -2: client timeout
        private static readonly HashSet<int> TransientErrorNumbers = new() { 40613, 40197, 40501, 49918, 4060, -2 };
        private const int MaxAttempts = 4;

        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Db") ?? throw new InvalidOperationException("Connection string 'Db' not found.");
        }

        public SqlConnection Create() => new SqlConnection(_connectionString);

        public async Task<SqlConnection> CreateOpenAsync()
        {
            for (var attempt = 1; ; attempt++)
            {
                var conn = new SqlConnection(_connectionString);
                try
                {
                    await conn.OpenAsync();
                    return conn;
                }
                catch (SqlException ex) when (attempt < MaxAttempts && IsTransient(ex))
                {
                    await conn.DisposeAsync();
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
                }
                catch
                {
                    await conn.DisposeAsync();
                    throw;
                }
            }
        }

        private static bool IsTransient(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
                if (TransientErrorNumbers.Contains(error.Number)) return true;
            return false;
        }
    }
}
