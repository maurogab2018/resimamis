using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IO;

namespace ResimamisBackend.Datos
{
    public static class ConnectionStringResolver
    {
        public static string Resolve(IConfiguration? configuration = null)
        {
            // Si no hay IConfiguration (por ejemplo, algunos repos hacen `new ApplicationDbContext()`),
            // armamos una configuración desde appsettings.json + variables de entorno.
            if (configuration is null)
            {
                var environment =
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                    "Production";

                configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    .Build();
            }

            // Render (y otros PaaS) típicamente provee DATABASE_URL.
            var fromEnv =
                Environment.GetEnvironmentVariable("DATABASE_URL") ??
                Environment.GetEnvironmentVariable("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(fromEnv))
                return NormalizeConnectionString(fromEnv);

            // Fallback para desarrollo/local.
            var fromConfig = configuration?.GetConnectionString("DefaultConnection") ??
                             configuration?["DefaultConnection"];

            if (!string.IsNullOrWhiteSpace(fromConfig))
                return NormalizeConnectionString(fromConfig);

            throw new InvalidOperationException(
                "Falta connection string. Configura 'ConnectionStrings:DefaultConnection' o la variable de entorno 'DATABASE_URL'.");
        }

        private static string NormalizeConnectionString(string connectionString)
        {
            if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertPostgresUrlToNpgsql(connectionString);
            }

            return connectionString;
        }

        // Convierte strings tipo postgres://user:pass@host:port/db?sslmode=require a string compatible con Npgsql.
        private static string ConvertPostgresUrlToNpgsql(string postgresUrl)
        {
            var uri = new Uri(postgresUrl);

            var userInfoParts = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfoParts[0]);
            var password = userInfoParts.Length == 2 ? Uri.UnescapeDataString(userInfoParts[1]) : "";

            var dbName = uri.AbsolutePath.TrimStart('/');

            var sslMode = ParseSslMode(uri.Query);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Username = username,
                Password = password,
                Database = dbName,
                SslMode = sslMode,
                // Evita problemas típicos con certificados en PaaS.
                TrustServerCertificate = true
            };

            return builder.ConnectionString;
        }

        private static SslMode ParseSslMode(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return SslMode.Require;

            var parts = query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2)
                    continue;

                if (!kv[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = kv[1].Trim().ToLowerInvariant();
                return value switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "require" => SslMode.Require,
                    "verify-ca" => SslMode.VerifyCA,
                    "verify-full" => SslMode.VerifyFull,
                    _ => SslMode.Require
                };
            }

            return SslMode.Require;
        }
    }
}

