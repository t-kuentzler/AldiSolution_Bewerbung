namespace Shared.Models;

public class ProductSalesViewModel
{
    public string ProductName { get; set; }
    public string ArticleNumber { get; set; }
    public int Sold { get; set; }       
    public int Returned { get; set; }
    public int TotalSold { get; set; } 
}