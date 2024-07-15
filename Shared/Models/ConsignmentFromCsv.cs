namespace Shared.Models;

public class ConsignmentFromCsv
{
    public string paket { get; set; }
    public string kdnr { get; set; }
    public string datum_druck { get; set; }
    public string lieferschein { get; set; }
    public string nve_nr { get; set; }
    public string kontrakt_nr_kunde { get; set; }
    public string name1 { get; set; }
    public string? name2 { get; set; }
    public string strasse { get; set; }
    public string nation { get; set; }
    public string plz { get; set; }
    public string ort { get; set; }
    public string? vers_text { get; set; }
    public string verpackungs_nr { get; set; }
    public string?  retoure_nr { get; set; }
    public string? farbe_id { get; set; }
    public string artikelnummer { get; set; }
    public string menge { get; set; }
}