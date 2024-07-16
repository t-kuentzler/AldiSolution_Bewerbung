using PdfSharp.Fonts;

namespace AldiOrderManagement.Helpers;

public class CustomFontResolver : IFontResolver
{
    public byte[] GetFont(string faceName)
    {
        // Lade die Schriftartdatei basierend auf dem Schriftartnamen
        if (faceName == "Arial")
        {
            string fontPath = "Fonts/arial.ttf"; 
            return File.ReadAllBytes(fontPath);
        }
        
        if (faceName == "Arial_bold")
        {
            string fontPath = "Fonts/arialbd.ttf"; 
            return File.ReadAllBytes(fontPath);
        }
       
        return null;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        return new FontResolverInfo(familyName);
    }
}