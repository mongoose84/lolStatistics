using MySqlConnector;

namespace RiotProxy.Infrastructure.External.Database
{
    public interface IV2DbConnectionFactory
    {
        MySqlConnection CreateConnection();
    }
}
