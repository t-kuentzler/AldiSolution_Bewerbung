namespace Shared.Contracts;

public interface IRmaNumberGenerator
{
    string GenerateRma(string orderCode);
}