using System.IO.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Entities;
using Shared.Factories;
using Shared.Generator;
using Shared.Helpers;
using Shared.Models;
using Shared.Repositories;
using Shared.Services;
using Shared.Validation;
using FluentValidation;
using System.Net.Mail;
using AldiOrderManagement.Validation;
using Microsoft.AspNetCore.Identity;
using PdfSharp.Fonts;
using Shared.Wrapper;
using FileWrapper = Shared.Helpers.FileWrapper;

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
            services.Configure<OAuthSettings>(options =>
            {
                configuration.GetSection("OAuthSettings").Bind(options);
                options.Secret = Environment.GetEnvironmentVariable("MAGMA_ALDI_OAUTH_CLIENTSECRET_TEST");
                options.Password = Environment.GetEnvironmentVariable("MAGMA_ALDI_OAUTH_PASSWORD_TEST");
            });

            services.Configure<EmailConfiguration>(options =>
            {
                configuration.GetSection("EmailConfiguration").Bind(options);
                options.SenderPassword = Environment.GetEnvironmentVariable("MAGMA_SMTP_PASSWORD");
            });

            services.Configure<DhlSettings>(options =>
            {
                configuration.GetSection("DhlSettings").Bind(options);
                options.ApiKey = Environment.GetEnvironmentVariable("MAGMA_DHL_API_KEY");
            });
            
            services.Configure<FileSettings>(options =>
            {
                configuration.GetSection("FileSettings").Bind(options);
            });
            
            services.Configure<CustomerSettings>(options =>
            {
                configuration.GetSection("CustomerSettings").Bind(options);
            });

            services.Configure<TrackingLinkBaseUrls>(options =>
            {
                configuration.GetSection("TrackingLinkBaseUrls").Bind(options);
            });

            // Services
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IAccessTokenService, AccessTokenService>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();
            services.AddScoped<IConsignmentService, ConsignmentService>();
            services.AddScoped<IConsignmentProcessingService, ConsignmentProcessingService>();
            services.AddScoped<ICsvFileService, CsvFileService>();
            services.AddScoped<IReturnProcessingService, ReturnProcessingService>();
            services.AddScoped<IReturnService, ReturnService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IDhlApiService, DhlApiService>();
            services.AddScoped<IDhlTrackingStatusService, DhlTrackingStatusService>();
            services.AddScoped<IShippedOrdersProcessingService, ShippedOrdersProcessingService>();
            services.AddScoped<IDpdTrackingDataService, DpdTrackingDataService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IQuantityCheckService, QuantityCheckService>();
            services.AddScoped<ICancellationService, CancellationService>();
            services.AddScoped<IExcelWorkbook, ExcelWorkbook>();
            services.AddScoped<IFileWrapper, FileWrapper>();
            services.AddScoped<IGuidGenerator, GuidGenerator>();
            services.AddSingleton<IFileMapping, FileMapping>();
            services.AddScoped<IImageLoader, ImageLoader>();
            services.AddScoped<IStatisticService, StatisticService>();
            services.AddScoped<IReturnConsignmentAndPackageService, ReturnConsignmentAndPackageService>();

            // Repositories
            services.AddScoped<IAccessTokenRepository, AccessTokenRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IConsignmentRepository, ConsignmentRepository>();
            services.AddScoped<IReturnRepository, ReturnRepository>();
            services.AddScoped<IStatisticRepository, StatisticRepository>();

            // Generator
            services.AddScoped<IRmaNumberGenerator, RandomRmaNumberGenerator>();

            // OAuth Client Service Factory
            services.AddScoped<IOAuthClientServiceFactory, OAuthClientServiceFactory>();

            // Registering the OAuthClientService with factory creation method
            services.AddScoped<IOAuthClientService>(provider =>
                provider.GetRequiredService<IOAuthClientServiceFactory>().Create());

            //Wrapper
            services.AddTransient(typeof(IValidatorWrapper<>), typeof(ValidatorWrapper<>));
            services.AddTransient(typeof(ISmtpClientWrapper), typeof(SmtpClientWrapper));

            // Validators
            services.AddTransient<IValidator<AccessToken>, AccessTokenValidator>();
            services.AddTransient<IValidator<DeliveryAddress>, DeliveryAddressValidator>();
            services.AddTransient<IValidator<OrderEntry>, OrderEntryValidator>();
            services.AddTransient<IValidator<Order>, OrderValidator>();
            services.AddTransient<IValidator<UpdateStatus>, UpdateStatusValidator>();
            services.AddTransient<IValidator<Consignment>, ConsignmentValidator>();
            services.AddTransient<IValidator<ConsignmentEntry>, ConsignmentEntryValidator>();
            services.AddTransient<IValidator<ShippingAddress>, ShippingAddressValidator>();
            services.AddTransient<IValidator<Address>, AddressValidator>();
            services.AddTransient<IValidator<CustomerInfo>, CustomerInfoValidator>();
            services.AddTransient<IValidator<ReturnConsignment>, ReturnConsignmentValidator>();
            services.AddTransient<IValidator<ReturnEntry>, ReturnEntryValidator>();
            services.AddTransient<IValidator<ReturnPackage>, ReturnPackageValidator>();
            services.AddTransient<IValidator<Return>, ReturnValidator>();
            services.AddTransient<IValidator<SearchTerm>, SearchTermValidator>();
            services.AddTransient<IValidator<CancelOrderEntryModel>, CancelOrderEntryValidator>();
            services.AddTransient<IValidator<ShipmentInfo>, ShipmentInfoValidator>();
            services.AddTransient<IValidator<ReceivingReturnAddressRequest>, ReceivingReturnAddressRequestValidator>();
            services.AddTransient<IValidator<ReceivingReturnConsignmentsRequest>, ReceivingReturnConsignmentsRequestValidator>();
            services.AddTransient<IValidator<ReceivingReturnCustomerInfoRequest>, ReceivingReturnCustomerInfoRequestValidator>();
            services.AddTransient<IValidator<ReceivingReturnEntriesRequest>, ReceivingReturnEntriesRequestValidator>();
            services.AddTransient<IValidator<ReceivingReturnPackagesRequest>, ReceivingReturnPackagesRequestValidator>();
            services.AddTransient<IValidator<ReceivingReturnRequest>, ReceivingReturnRequestValidator>();
            services.AddTransient<IValidator<ProcessCancellationEntry>, ProcessCancellationEntryValidator>();
            services.AddTransient<IValidator<ReceivingReturnAddressResponse>, ReceivingReturnAddressResponseValidator>();
            services.AddTransient<IValidator<ReceivingReturnConsignmentsResponse>, ReceivingReturnConsignmentsResponseValidator>();
            services.AddTransient<IValidator<ReceivingReturnCustomerInfoResponse>, ReceivingReturnCustomerInfoResponseValidator>();
            services.AddTransient<IValidator<ReceivingReturnEntriesResponse>, ReceivingReturnEntriesResponseValidator>();
            services.AddTransient<IValidator<ReceivingReturnPackagesResponse>, ReceivingReturnPackagesResponseValidator>();
            services.AddTransient<IValidator<ReceivingReturnResponse>, ReceivingReturnResponseValidator>();

            // Font Resolver
            services.AddSingleton<IFontResolver, CustomFontResolver>();
            services.AddSingleton<IFontResolver>(provider => provider.GetRequiredService<CustomFontResolver>());

            services.AddSingleton<SmtpClient>();
            
            services.AddSingleton<IFileSystem, FileSystem>();

        }
    }
}
