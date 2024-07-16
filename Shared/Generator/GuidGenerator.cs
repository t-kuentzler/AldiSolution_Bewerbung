using Shared.Contracts;

namespace Shared.Generator;

public class GuidGenerator : IGuidGenerator
{
    public Guid NewGuid()
    {
        return Guid.NewGuid();
    }
}