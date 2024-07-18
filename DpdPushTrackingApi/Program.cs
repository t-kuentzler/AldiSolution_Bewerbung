using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Logger;
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
        var sharedConfigPath = GetSharedAppSettingsPath();

        builder.Configuration
            .AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

        // Register shared services
        builder.Services.AddSharedServices(builder.Configuration);

        // Define rate limiting options directly in the code
        var rateLimitingOptions = new RateLimitingOptions
        {
            TokenLimit = 1,
            TokensPerPeriod = 1,
            ReplenishmentPeriodInSeconds = 1
        };

        // Add rate limiting services
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: "global",
                    factory: _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = rateLimitingOptions.TokenLimit,
                        TokensPerPeriod = rateLimitingOptions.TokensPerPeriod,
                        ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitingOptions.ReplenishmentPeriodInSeconds),
                        AutoReplenishment = true,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

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
    
    private static string GetSharedAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var solutionRoot = Directory.GetParent(currentDirectory)?.FullName;

        if (solutionRoot == null)
        {
            throw new DirectoryNotFoundException("Solution root directory not found.");
        }

        var sharedConfigPath = Path.Combine(solutionRoot, "Shared", "appsettings.json");
        if (!File.Exists(sharedConfigPath))
        {
            throw new FileNotFoundException($"The configuration file '{sharedConfigPath}' was not found and is not optional.");
        }

        return sharedConfigPath;
    }
}

public class RateLimitingOptions
{
    public int TokenLimit { get; set; }
    public int TokensPerPeriod { get; set; }
    public int ReplenishmentPeriodInSeconds { get; set; }
}
