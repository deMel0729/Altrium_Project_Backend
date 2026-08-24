// written by the whole team 
using Microsoft.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
namespace Altrium_Project_Backend.Data

// connecting the database with the backend using the connection string from the appsettings.json file

{
    public interface IDbConnectionFactory
    {
        SqlConnection Create();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Db") ?? throw new InvalidOperationException("Connection string 'Db' not found.");
        }

        public SqlConnection Create() => new SqlConnection(_connectionString);
    }
}
