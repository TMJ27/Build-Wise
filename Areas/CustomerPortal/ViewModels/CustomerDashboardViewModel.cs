namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class CustomerDashboardViewModel
{
    public string? CustomerName { get; set; }
    public int RequestCount { get; set; }
    public int OpenRequestCount { get; set; }
    public int ApprovedQuotationCount { get; set; }
    public IReadOnlyList<CustomerRequestRowViewModel> Requests { get; set; } = [];
    public IReadOnlyList<CustomerQuotationRowViewModel> Quotations { get; set; } = [];
}

public sealed class CustomerRequestRowViewModel
{
    public Guid Id { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime CreationDate { get; set; }
}

public sealed class CustomerQuotationRowViewModel
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int RevisionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IncludesPrices { get; set; }
    public decimal? Total { get; set; }
    public string? CurrencyCode { get; set; }
}
