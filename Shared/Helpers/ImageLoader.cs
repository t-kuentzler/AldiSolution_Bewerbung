using PdfSharp.Drawing;
using Shared.Contracts;

namespace Shared.Helpers;

public class ImageLoader : IImageLoader
{
    public XImage LoadImage(string path)
    {
        return XImage.FromFile(path);
    }
}