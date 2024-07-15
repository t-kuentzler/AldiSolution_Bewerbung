using Shared.Contracts;

namespace Shared.Generator;

public class RandomRmaNumberGenerator : IRmaNumberGenerator
{
    private Random _random;

    public RandomRmaNumberGenerator()
    {
        _random = new Random();
    }

    public string GenerateRma(string orderCode)
    {
        int randomNumber = _random.Next(10000, 100000);
        string rma = $"{orderCode}-{randomNumber}";
        return rma;
    }
}