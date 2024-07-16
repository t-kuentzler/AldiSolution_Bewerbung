using Shared.Constants;

namespace Shared.Mappings;

public class ReasonCodeMapping
{
    private static readonly Dictionary<string, string> _reasons = new Dictionary<string, string>
    {
        {SharedStatus.DamagedIntransit, "Auf Transportweg beschädigt"},
        {SharedStatus.DontLikeAnyMore, "Gefällt nicht mehr"},
        {SharedStatus.ItemToBigOrSmall, "Artikel zu groß oder klein"},
        {SharedStatus.FoundCheaperPrice, "Günstigeren Preis entdeckt"},
        {SharedStatus.GoodWill, "Kulanz"},
        {SharedStatus.ItemBrokenOrDamaged, "Artikel ist defekt / beschädigt"},
        {SharedStatus.LateDelivery, "Späte Lieferung"},
        {SharedStatus.LostIntransit, "Auf Transportweg verloren"},
        {SharedStatus.ManufacturingFault, "Herstellerfehler"},
        {SharedStatus.MisPickItemMissing, "Artikel fehlt"},
        {SharedStatus.MisPickWrongItemDelivered, "Verpasster Link-Deal"},
        {SharedStatus.MissingPartsOrAccessoriesMissing, "Fehlende Teile / Zubehör fehlt"},
        {SharedStatus.PriceMatch, "Preisübereinstimmung"},
        {SharedStatus.ShippingPackagingDamaged, "Versandverpackung beschädigt"},
        {SharedStatus.SiteError, "Standortfehler"},
        {SharedStatus.WrongDescription, "Falsche Beschreibung"},
    };
    
    public static string GetReasonDescription(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return "Unbekannter Grund";
            
        }
        if (_reasons.TryGetValue(code, out var description))
        {
            return description;
        }
        return "Unbekannter Grund";
    }
}