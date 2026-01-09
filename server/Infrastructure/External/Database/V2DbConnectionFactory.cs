using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class V2DbConnectionFactory : IV2DbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionStringV2))
                throw new InvalidOperationException("Database v2 connection string is not configured. Set LOL_DB_CONNECTIONSTRING_V2 or a connection string named 'DatabaseV2'.");

            return new MySqlConnection(Secrets.DatabaseConnectionStringV2);
        }
    }
}
