using Shared;
using Shared.Logger;
using System.Net;
using System.Threading.RateLimiting;
using DpdPushTrackingApi.Models;

namespace DpdPushTrackingApi;

public class Program
{
    public static void Main(string[] args)
    {
        LoggerConfigurator.ConfigureLogger();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

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

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        // Add rate limiting middleware globally
        app.UseRateLimiter();

        app.Run();
    }
}
