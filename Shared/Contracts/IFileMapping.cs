namespace Shared.Contracts;

public interface IFileMapping
{
    string? GetFilePath(string fileId);
    void SetFilePath(string fileId, string path);
}