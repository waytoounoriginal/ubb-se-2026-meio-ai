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
        /// Creates and opens a new <see cref="SqlConnection"/>.
        /// </summary>
        Task<SqlConnection> CreateConnectionAsync();
    }
}
