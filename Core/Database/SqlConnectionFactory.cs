using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{

    public class SqlConnectionFactory : ISqlConnectionFactory
    {
    
        private const string DatabaseName = "MeioAiDb";
        private const string DefaultConnectionString =
            @"Server=.\MEIOAI;" +
            "Database=" + DatabaseName + ";" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        private const string MasterConnectionString =
            @"Server=.\MEIOAI;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

        private readonly string _connectionString;

        public SqlConnectionFactory(string? connectionString = null)
        {
            _connectionString = connectionString ?? DefaultConnectionString;
        }

        public async Task<SqlConnection> CreateConnectionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<SqlConnection> CreateMasterConnectionAsync()
        {
            var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}
