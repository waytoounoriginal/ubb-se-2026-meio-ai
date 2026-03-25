using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{
    /// <summary>
    /// Default implementation of <see cref="ISqlConnectionFactory"/> 
    /// that creates connections to a local SQL Server instance.
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        // Use LocalDB instead of a full SQL server instance, as it is installed by default with Visual Studio
        private const string DefaultConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=MeioAiDb;Trusted_Connection=True;TrustServerCertificate=True;";

        private const string MasterConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

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