using Serilog;
using Shared.Contracts;

namespace Shared.Logger;

public class AppLogger : IAppLogger
{
    private readonly ILogger _logger;

    public AppLogger()
    {
        _logger = Log.Logger;
    }

    public void LogInformation(string message, params object[] args)
    {
        _logger.Information(message, args);
    }

    public void LogWarning(string message, params object[] args)
    {
        _logger.Warning(message, args);
    }

    public void LogError(string message, params object[] args)
    {
        _logger.Error(message, args);
    }
}