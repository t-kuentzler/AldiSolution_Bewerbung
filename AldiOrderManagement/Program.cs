using Microsoft.AspNetCore.Identity;
using Serilog;
using Shared;
using Shared.Logger;
using ApplicationDbContext = AldiOrderManagement.Data.ApplicationDbContext;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.InteropServices;

namespace AldiOrderManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoggerConfigurator.ConfigureLogger();

            Log.Information("Die Anwendung wurde gestartet.");

            var builder = WebApplication.CreateBuilder(args);

            // Load shared configurations
            var sharedConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            builder.Configuration
                .AddJsonFile(sharedConfigPath, optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Add services to the container.
            builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddControllersWithViews();
            builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            // Data Protection Configuration
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Konfiguration für Windows
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(@"\\10.4.240.76\aspnet_share\antiforgerytoken\AldiOrderManagement_TEST"))
                    .SetApplicationName("AldiOrderManagement_TEST");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Konfiguration für macOS
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/Volumes/aspnet_share/antiforgerytoken/AldiOrderManagement_TEST"))
                    .SetApplicationName("AldiOrderManagement_TEST");
            }

            // Register shared services
            builder.Services.AddSharedServices(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add Anti-Forgery Middleware
            app.Use((context, next) =>
            {
                var tokens = context.RequestServices.GetService<IAntiforgery>().GetAndStoreTokens(context);
                context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken,
                    new CookieOptions { HttpOnly = false });
                return next();
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
