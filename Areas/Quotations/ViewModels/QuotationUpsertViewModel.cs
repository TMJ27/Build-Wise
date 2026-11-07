using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public sealed class QuotationUpsertViewModel
{
    public Guid? Id { get; set; }
    public Guid? RevisionId { get; set; }
    public string? Version { get; set; }
    public Guid RequestId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = string.Empty;

    public bool IncludesPrices { get; set; } = true;

    [StringLength(3)]
    public string? CurrencyCode { get; set; }

    [Range(typeof(decimal), "0", "999999999999.99")]
    public decimal TaxAmount { get; set; }

    [DataType(DataType.Date)]
    public DateTime? IssueDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }

    [StringLength(4000)]
    public string? Terms { get; set; }

    [StringLength(4000)]
    public string? Exclusions { get; set; }

    public List<QuotationItemInputViewModel> Items { get; set; } = [];
}

public sealed class QuotationItemInputViewModel
{
    public Guid ScopeItemId { get; set; }

    [Required, StringLength(50)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 1000000)]
    public int? Quantity { get; set; }

    [Range(typeof(decimal), "0", "999999999999.99")]
    public decimal? LineAmount { get; set; }
}
