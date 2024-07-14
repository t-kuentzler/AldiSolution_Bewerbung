using System.Data;
using System.Net.Mail;
using Azure.Core;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared;

public static class ServiceCollectionExtensions
    {
        public static void AddSharedServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Konfiguration des Datenbankkontexts mit SQL
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }
            else
            {
                throw new InvalidOperationException("Die Verbindungszeichenfolge des ConnectionString wurde nicht gefunden.");
            }

            // // Repository-Dienste
            // services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();
            // services.AddScoped<IOrderRepository, OrderRepository>();
            // services.AddScoped<IConsignmentRepository, ConsignmentRepository>();
            // services.AddScoped<IReturnRepository, ReturnRepository>();
            //
            // // Anwendungs-Dienste
            // services.AddScoped<IAccessTokenService, AccessTokenService>();
            // services.AddScoped<IOAuthClientService, OAuthClientService>();
            // services.AddScoped<IOrderService, OrderService>();
            // services.AddScoped<IConsignmentService, ConsignmentService>();
            // services.AddScoped<IReturnService, ReturnService>();
            // services.AddScoped<ICsvFileService, CsvFileService>();
            // services.AddScoped<IEmailService, EmailService>();
            // services.AddScoped<IRmaNumberGenerator, RandomRmaNumberGenerator>();
            //
            // // FluentValidation Validators
            // services.AddTransient<IValidator<AccessToken>, AccessTokenValidator>();
            // services.AddTransient<IValidator<Order>, OrderValidator>();
            // services.AddTransient<IValidator<OrderEntry>, OrderEntryValidator>();
            // services.AddTransient<IValidator<DeliveryAddress>, DeliveryAddressValidator>();
            // services.AddTransient<IValidator<UpdateStatus>, UpdateStatusValidator>();
            // services.AddTransient<IValidator<CustomerInfo>, CustomerInfoValidator>();
            // services.AddTransient<IValidator<ReturnConsignment>, ReturnConsignmentValidator>();
            // services.AddTransient<IValidator<ReturnEntry>, ReturnEntryValidator>();
            // services.AddTransient<IValidator<Return>, ReturnValidator>();
            // services.AddTransient<IValidator<Address>, AddressValidator>();
            // services.AddTransient<IValidator<ReturnPackage>, ReturnPackageValidator>();
            // services.AddTransient<IValidator<Consignment>, ConsignmentValidator>();
            // services.AddTransient<IValidator<ConsignmentEntry>, ConsignmentEntryValidator>();
            // services.AddTransient<IValidator<ShippingAddress>, ShippingAddressValidator>();
            //
            // // Konfigurationseinstellungen
            // services.Configure<OAuthSettings>(configuration.GetSection("OAuthSettings"));
            // services.Configure<EmailConfiguration>(configuration.GetSection("EmailConfiguration"));
            //
            // // OAuth Client Service Factory
            // services.AddSingleton<OAuthClientServiceFactory>();
            // services.AddScoped<IOAuthClientService>(provider =>
            //     provider.GetRequiredService<OAuthClientServiceFactory>().Create());
            // services.AddSingleton<SmtpClient>();
        }
    }