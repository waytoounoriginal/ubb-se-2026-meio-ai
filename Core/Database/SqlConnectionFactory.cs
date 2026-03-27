using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{

    public class SqlConnectionFactory : ISqlConnectionFactory
    {
    
        private const string DatabaseName = "MeioAiDb1";
        private const string DefaultConnectionString =
            @"Data Source=LENOVOLOQIN\MEIOAI;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=""SQL Server Management Studio"";Command Timeout=0";


        private const string MasterConnectionString =
            @"Data Source=LENOVOLOQIN\MEIOAI;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=""SQL Server Management Studio"";Command Timeout=0";

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
