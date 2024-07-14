using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Contracts;
using Shared.Models;
using Shared.Services;

namespace Shared.Factories;

public class OAuthClientServiceFactory : IOAuthClientServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OAuthClientServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IOAuthClientService Create()
    {
        return new OAuthClientService(
            _serviceProvider.GetRequiredService<IOptions<OAuthSettings>>(),
            _serviceProvider.GetRequiredService<IAccessTokenService>(),
            _serviceProvider.GetRequiredService<ILogger<OAuthClientService>>(),
            _serviceProvider.GetRequiredService<IHttpClientFactory>()
            
        );
    }
}