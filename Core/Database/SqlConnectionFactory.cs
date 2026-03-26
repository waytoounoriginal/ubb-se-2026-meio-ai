using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{
    /// <summary>
    /// Default implementation of <see cref="ISqlConnectionFactory"/> 
    /// that creates connections to a local SQL Server instance.
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        // TODO: Move to appsettings or a configuration file before release.
        private const string DefaultConnectionString =
            "Server=localhost;Database=MeioAiDb;Trusted_Connection=True;TrustServerCertificate=True;";

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
    }
}
