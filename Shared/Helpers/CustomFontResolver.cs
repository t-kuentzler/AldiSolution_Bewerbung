using Microsoft.Extensions.Options;
using PdfSharp.Fonts;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.IO;

namespace Shared.Helpers
{
    public class CustomFontResolver : IFontResolver
    {
        private readonly string _fontsFolder;
        private readonly ILogger<CustomFontResolver> _logger;

        public CustomFontResolver(IOptions<FileSettings> fileSettings, ILogger<CustomFontResolver> logger)
        {
            if (fileSettings == null || fileSettings.Value == null)
            {
                throw new InvalidOperationException("FileSettings cannot be null");
            }

            _fontsFolder = GetFontsFolder(fileSettings.Value.FontsFolder);
            _logger = logger;
        }

        private string GetFontsFolder(string configuredPath)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (env == "Development")
            {
                //Fonts liegt in Shared
                var solutionRoot = Path.Combine(Directory.GetCurrentDirectory(),"..", "Shared", "Fonts");
                return Path.GetFullPath(solutionRoot);
            }
            else
            {
                //Fonts liegt direkt im Veröffentlichungsverzeichnis
                return Path.Combine(AppContext.BaseDirectory, configuredPath);
            }
        }

        public byte[] GetFont(string faceName)
        {
            string fontPath;

            switch (faceName)
            {
                case "Arial":
                    fontPath = Path.Combine(_fontsFolder, "arial.ttf");
                    _logger.LogInformation($"Pfad der Font '{faceName}': '{fontPath}'");
                    break;
                case "Arial_bold":
                    fontPath = Path.Combine(_fontsFolder, "arialbd.ttf");
                    _logger.LogInformation($"Pfad der Font '{faceName}': '{fontPath}'");
                    break;
                default:
                    _logger.LogError($"Font mit dem Namen '{faceName}' ist in der Auflistung nicht vorhanden. Es wird null zurückgegeben.");
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
