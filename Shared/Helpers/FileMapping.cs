using System.Collections.Concurrent;
using Shared.Contracts;

namespace Shared.Helpers;

public class FileMapping : IFileMapping
{
    private ConcurrentDictionary<string, string> _fileMappings = new ConcurrentDictionary<string, string>();

    public string? GetFilePath(string fileId)
    {
        _fileMappings.TryGetValue(fileId, out var filePath);
        return filePath;
    }

    public void SetFilePath(string fileId, string path)
    {
        _fileMappings[fileId] = path;
    }
}