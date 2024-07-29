using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Entities;
using Shared.Generator;
using Shared.Helpers;
using Shared.Models;
using Shared.Repositories;
using Shared.Services;
using Shared.Validation;
using FluentValidation;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using PdfSharp.Fonts;
using Shared.Wrapper;

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
                
                services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();
            }
            else
            {
                throw new InvalidOperationException(
                    "Die Verbindungszeichenfolge wurde nicht in der Umgebungsvariablen gefunden.");
            }

            // Add HttpClient factory
            services.AddHttpClient();

            // Appsettings
            services.Configure<FileSettings>(options =>
            {
                configuration.GetSection("FileSettings").Bind(options);
            });
            
            // Services
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();
            
            // Repositories
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Generator
            services.AddScoped<IRmaNumberGenerator, RandomRmaNumberGenerator>();
            services.AddScoped<IGuidGenerator, GuidGenerator>();

            //Wrapper
            services.AddTransient(typeof(IValidatorWrapper<>), typeof(ValidatorWrapper<>));

            // Validators
            services.AddTransient<IValidator<DeliveryAddress>, DeliveryAddressValidator>();
            services.AddTransient<IValidator<OrderEntry>, OrderEntryValidator>();
            services.AddTransient<IValidator<Order>, OrderValidator>();

            // Font Resolver
            services.AddSingleton<IFontResolver, CustomFontResolver>();
            services.AddSingleton<IFontResolver>(provider => provider.GetRequiredService<CustomFontResolver>());

            services.AddSingleton<SmtpClient>();
            
            services.AddSingleton<IFileSystem, FileSystem>();

        }
    }
}
