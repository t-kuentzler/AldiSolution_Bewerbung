namespace Shared.Contracts;

public interface IOAuthClientServiceFactory
{
    IOAuthClientService Create();
}