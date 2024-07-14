using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shared;
using Shared.Logger;

namespace AldiApplication
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Logger konfigurieren
            LoggerConfigurator.ConfigureLogger();

            var host = CreateHostBuilder(args).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Die Anwendung wird gestartet...");

            await CheckDatabaseConnection(host);

            // await ExecuteOrderProcessAsync(host.Services);
            // await ReadAndSaveConsignmentsAsync(host.Services);
            // await ReadAndSaveReturnsAsync(host.Services);

            Log.CloseAndFlush();
        }

        private static async Task CheckDatabaseConnection(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var dbContext = services.GetRequiredService<ApplicationDbContext>();
                    var canConnect = await dbContext.Database.CanConnectAsync();
                    if (canConnect)
                    {
                        Log.Information("Verbindung zur Datenbank erfolgreich.");
                    }
                    else
                    {
                        Log.Error("Fehler bei der Verbindung zur Datenbank.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ein unerwarteter Fehler ist aufgetreten beim Versuch, die Datenbankverbindung zu prüfen.");
                }
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

                    builder.AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true)
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) => { ConfigureServices(services, context.Configuration); })
                .UseSerilog()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                });

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSharedServices(configuration);
        }
        
        
    }
}
