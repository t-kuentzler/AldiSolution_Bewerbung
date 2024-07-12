using Serilog;

namespace Shared.Logger;

public static class LoggerConfigurator
{
    public static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Error)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}