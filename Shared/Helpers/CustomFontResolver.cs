using Microsoft.Extensions.Options;
using PdfSharp.Fonts;
using System.IO;
using Shared.Models;

namespace Shared.Helpers
{
    public class CustomFontResolver : IFontResolver
    {
        private readonly string _fontsFolder;

        public CustomFontResolver(IOptions<FileSettings> fileSettings)
        {
            if (fileSettings == null || fileSettings.Value == null)
            {
                throw new InvalidOperationException("FileSettings cannot be null");
            }
            _fontsFolder = Path.GetFullPath(fileSettings.Value.FontsFolder);
        }

        public byte[] GetFont(string faceName)
        {
            string fontPath;

            switch (faceName)
            {
                case "Arial":
                    fontPath = Path.Combine(_fontsFolder, "arial.ttf");
                    break;
                case "Arial_bold":
                    fontPath = Path.Combine(_fontsFolder, "arialbd.ttf");
                    break;
                default:
                    return null;
            }

            if (!File.Exists(fontPath))
            {
                throw new FileNotFoundException($"Font file not found: {fontPath}");
            }

            return File.ReadAllBytes(fontPath);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(familyName);
        }
    }
}