using Shared.Contracts;

namespace Shared.Helpers;

public class FileWrapper : IFileWrapper
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
    public bool DirectoryExists(string path) => Directory.Exists(path);
}