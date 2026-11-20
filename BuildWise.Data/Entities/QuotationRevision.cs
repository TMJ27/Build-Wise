using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class QuotationRevision
{
    public Guid Id { get; set; }

    public Guid QuotationId { get; set; }

    public int RevisionNumber { get; set; }

    public int StatusId { get; set; }

    public string Title { get; set; } = null!;

    public string CompanyNameSnapshot { get; set; } = null!;

    public string? CompanyEmailSnapshot { get; set; }

    public string? CompanyPhoneSnapshot { get; set; }

    public string? CompanyAddressSnapshot { get; set; }

    public string? CompanyRegistrationNumberSnapshot { get; set; }

    public string? CompanyTaxNumberSnapshot { get; set; }

    public string? CompanyLogoStorageKeySnapshot { get; set; }

    public string CustomerNameSnapshot { get; set; } = null!;

    public string? CustomerEmailSnapshot { get; set; }

    public string? CustomerPhoneSnapshot { get; set; }

    public string? CustomerAddressSnapshot { get; set; }

    public string SiteAddressSnapshot { get; set; } = null!;

    public string RequestDescriptionSnapshot { get; set; } = null!;

    public string? ClarificationsSnapshotJson { get; set; }

    public string? Terms { get; set; }

    public string? Exclusions { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ValidUntil { get; set; }

    public bool IncludesPrices { get; set; }

    public string? CurrencyCode { get; set; }

    public int? TaxTreatmentId { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? Total { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdate { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? ApprovedDocumentJson { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual Quotation Quotation { get; set; } = null!;

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
}
