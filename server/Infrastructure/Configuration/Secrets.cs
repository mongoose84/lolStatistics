namespace RiotProxy.Infrastructure
{
    public static class Secrets
    {
        private static bool _initialized = false;
        private static readonly string SecretsFolder = AppDomain.CurrentDomain.BaseDirectory;

        public static string ApiKey { get; private set; } = string.Empty;
        public static string DatabaseConnectionString { get; private set; } = string.Empty;
        public static string DatabaseConnectionStringV2 { get; private set; } = string.Empty;

        public static void Initialize()
        {
            if (_initialized)
            {
                // Idempotent in case the host is created multiple times (e.g., integration tests)
                return;
            }

            DatabaseConnectionString = Read("DatabaseSecret.txt");
            // Optional: new v2 database connection string for migration/cutover
            DatabaseConnectionStringV2 = TryRead("DatabaseSecretV2.txt");

            ApiKey = Read("RiotSecret.txt");

            _initialized = true;
        }
        
        private static string Read(string filename)
        {
            string filePath = Path.Combine(SecretsFolder, filename);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Secret file not found: {filename}");
            }

            return File.ReadAllText(filePath).Trim();
        }

        private static string TryRead(string filename)
        {
            string filePath = Path.Combine(SecretsFolder, filename);
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }
            return File.ReadAllText(filePath).Trim();
        }
        
    }
}