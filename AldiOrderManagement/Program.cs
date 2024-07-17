using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Logger;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using System.Runtime.InteropServices;

namespace AldiOrderManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
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
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages(); 
            builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

            // Data Protection Configuration
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(@"\\10.4.240.76\aspnet_share\antiforgerytoken\AldiOrderManagement_TEST"))
                    .SetApplicationName("AldiOrderManagement_TEST");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo("/Volumes/aspnet_share/antiforgerytoken/AldiOrderManagement_TEST"))
                    .SetApplicationName("AldiOrderManagement_TEST");
            }

            // Register shared services
            builder.Services.AddSharedServices(builder.Configuration);

            var app = builder.Build();

            // Check database connection
            await CheckDatabaseConnection(app);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Order}/{action=Index}/{id?}");
            app.MapRazorPages();

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
}
