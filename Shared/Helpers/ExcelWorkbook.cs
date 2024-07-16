using ClosedXML.Excel;
using Shared.Contracts;

namespace Shared.Helpers;

public class ExcelWorkbook : IExcelWorkbook
{
    private XLWorkbook _workbook;

    public ExcelWorkbook()
    {
        _workbook = new XLWorkbook();
    }

    public IXLWorksheet AddWorksheet(string name)
    {
        return _workbook.Worksheets.Add(name);
    }

    public void SaveAs(Stream stream)
    {
        _workbook.SaveAs(stream);
    }
}