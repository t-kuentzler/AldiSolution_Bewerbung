using ClosedXML.Excel;

namespace Shared.Contracts;

public interface IExcelWorkbook
{
    IXLWorksheet AddWorksheet(string name);
    void SaveAs(Stream stream);
}