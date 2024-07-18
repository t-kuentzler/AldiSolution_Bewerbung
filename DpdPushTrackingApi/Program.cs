using Shared;
using Shared.Logger;
using System.Net;
using System.Threading.RateLimiting;
using DpdPushTrackingApi.Models;
using Serilog;

namespace DpdPushTrackingApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        LoggerConfigurator.ConfigureLogger();
        Log.Information("Die Anwendung wurde gestartet.");

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog();

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();

        // Load shared configurations
        var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        builder.Configuration
            .AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Register shared services
        builder.Services.AddSharedServices(builder.Configuration);

        // Load allowed IP addresses
        var allowedIpAddresses = builder.Configuration.GetSection("AllowedIPAddresses").Get<List<string>>() ?? new List<string>();
        var parsedIpAddresses = allowedIpAddresses.Select(IPAddress.Parse).ToList();

        // Load rate limiting options with default values
        var rateLimitingOptions = builder.Configuration.GetSection("RateLimiting").Get<RateLimitingOptions>() ?? new RateLimitingOptions
        {
            TokenLimit = 4,
            TokensPerPeriod = 4,
            ReplenishmentPeriodInSeconds = 1
        };

        // Add rate limiting services
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(httpContext =>
            {
                var clientIp = httpContext.Connection.RemoteIpAddress;
                if (clientIp == null || !parsedIpAddresses.Contains(clientIp))
                {
                    // If the IP address is not allowed, no limiter will be applied
                    return RateLimitPartition.GetNoLimiter(clientIp ?? IPAddress.None);
                }

                return RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: clientIp,
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = rateLimitingOptions.TokenLimit,
                        TokensPerPeriod = rateLimitingOptions.TokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitingOptions.ReplenishmentPeriodInSeconds),
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
            });

            options.OnRejected = (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = 429; // Too Many Requests
                return new ValueTask();
            };
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        await CheckDatabaseConnection(app);

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        app.MapControllers();

        // Add rate limiting middleware globally
        app.UseRateLimiter();

        app.UseRouting();
        
        app.Run();
    }
    
    private static async Task CheckDatabaseConnection(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
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
}
