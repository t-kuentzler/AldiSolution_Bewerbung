namespace Shared.Contracts;

public interface IFileWrapper
{
    void CreateDirectory(string path);
    void WriteAllBytes(string path, byte[] bytes);
    bool DirectoryExists(string path);
}