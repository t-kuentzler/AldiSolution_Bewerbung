using PdfSharp.Drawing;

namespace Shared.Contracts;

public interface IImageLoader
{
    XImage LoadImage(string path);
}