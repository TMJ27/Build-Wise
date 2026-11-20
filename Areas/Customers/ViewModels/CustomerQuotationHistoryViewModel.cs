using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Customers.ViewModels;

public class CustomerQuotationHistoryViewModel
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public QuotationStatus Status { get; set; }
    public bool IncludesPrices { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Total { get; set; }
}

public sealed record CustomerApprovedValueViewModel(string CurrencyCode, decimal Total);
