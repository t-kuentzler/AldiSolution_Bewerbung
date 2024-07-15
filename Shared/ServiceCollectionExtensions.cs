using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Entities;
using Shared.Factories;
using Shared.Models;
using Shared.Repositories;
using Shared.Services;
using Shared.Validation;
using AccessTokenService = Shared.Services.AccessTokenService;

namespace Shared
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSharedServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = Environment.GetEnvironmentVariable("MAGMA_ALDI_CONNECTIONSTRING_TEST");

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            else
            {
                throw new InvalidOperationException("Die Verbindungszeichenfolge wurde nicht in der Umgebungsvariablen gefunden.");
            }

            // Add HttpClient factory
            services.AddHttpClient();

            // Services
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IAccessTokenService, AccessTokenService>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();

            // Repositories
            services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            // OAuth Client Service Factory
            services.AddSingleton<IOAuthClientServiceFactory, OAuthClientServiceFactory>();

            // Registering the OAuthClientService with factory creation method
            services.AddScoped(provider => 
                provider.GetRequiredService<IOAuthClientServiceFactory>().Create());

            // Validator Wrapper
            services.AddTransient(typeof(IValidatorWrapper<>), typeof(ValidatorWrapper<>));

            // Validators
            services.AddTransient<IValidator<AccessToken>, AccessTokenValidator>();
            services.AddTransient<IValidator<DeliveryAddress>, DeliveryAddressValidator>();
            services.AddTransient<IValidator<OrderEntry>, OrderEntryValidator>();
            services.AddTransient<IValidator<Order>, OrderValidator>();
            services.AddTransient<IValidator<UpdateStatus>, UpdateStatusValidator>();
        }
    }
}
