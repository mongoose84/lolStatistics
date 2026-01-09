using RiotProxy.Infrastructure;

namespace RiotProxy.Application.Endpoints.Diagnostics
{
    /// <summary>
    /// Diagnostics endpoint for verifying configuration and connectivity.
    /// Returns status of critical infrastructure: database, API keys, etc.
    /// </summary>
    public sealed class DiagnosticsEndpoint : IEndpoint
    {
        public string Route { get; }

        public DiagnosticsEndpoint(string basePath)
        {
            Route = basePath + "/diagnostics";
        }

        public void Configure(WebApplication app)
        {
            app.MapGet(Route, (HttpContext httpContext) =>
            {
                var isApiKeyConfigured = !string.IsNullOrWhiteSpace(Secrets.ApiKey);
                var isDbConfigured = !string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionString);
                var isDbV2Configured = !string.IsNullOrWhiteSpace(Secrets.DatabaseConnectionStringV2);

                var diagnostics = new
                {
                    environment = GetEnvironment(),
                    timestamp = DateTime.UtcNow,
                    configuration = new
                    {
                        apiKeyConfigured = isApiKeyConfigured,
                        databaseConfigured = isDbConfigured,
                        databaseV2Configured = isDbV2Configured,
                        allConfigured = isApiKeyConfigured && isDbConfigured && isDbV2Configured
                    },
                    notes = new string[]
                    {
                        "If 'allConfigured' is false, check README.md for environment variable setup instructions.",
                        "Required env vars: RIOT_API_KEY, LOL_DB_CONNECTIONSTRING, LOL_DB_CONNECTIONSTRING_V2",
                        "Or set in appsettings.json under ConnectionStrings section."
                    }
                };

                return Results.Ok(diagnostics);
            });
        }

        private static string GetEnvironment()
        {
            var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return !string.IsNullOrWhiteSpace(aspnetEnv) ? aspnetEnv : "Production";
        }
    }
}
