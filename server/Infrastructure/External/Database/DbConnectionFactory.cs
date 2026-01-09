using MySqlConnector; 

namespace RiotProxy.Infrastructure.External.Database
{
    public sealed class DbConnectionFactory : IDbConnectionFactory
    {
        public MySqlConnection CreateConnection()
        {
            if (string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString))
                throw new InvalidOperationException("Database connection string is not configured. Set LOL_DB_CONNECTIONSTRING or a connection string named 'Default'.");

            return new MySqlConnection(Secrets.DatabaseConnectionString);
        }
    }
}