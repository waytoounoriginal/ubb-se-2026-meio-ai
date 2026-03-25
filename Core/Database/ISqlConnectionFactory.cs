using Microsoft.Data.SqlClient;

namespace ubb_se_2026_meio_ai.Core.Database
{
    /// <summary>
    /// Factory abstraction for creating SQL Server connections.
    /// Allows features to obtain connections without knowing the connection string.
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        /// Creates and opens a new <see cref="SqlConnection"/> to the target database.
        /// </summary>
        Task<SqlConnection> CreateConnectionAsync();

        /// <summary>
        /// Creates and opens a new <see cref="SqlConnection"/> to the master database
        /// for server-level operations like CREATE DATABASE.
        /// </summary>
        Task<SqlConnection> CreateMasterConnectionAsync();
    }
}
