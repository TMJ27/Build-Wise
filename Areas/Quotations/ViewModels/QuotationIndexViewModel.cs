using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public class QuotationIndexViewModel
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public QuotationStatus? Status { get; set; }
    public bool IncludesPrices { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Total { get; set; }
    public DateTime? CreationDate { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}
