using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ClientRequest
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public string? ReferenceNumber { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string SiteAddress { get; set; } = null!;

    public string? PropertyType { get; set; }

    public int StatusId { get; set; }

    public Guid CreatorId { get; set; }

    public Guid? ConfirmedBy { get; set; }

    public DateTime? ConfirmationDate { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual ICollection<ScopeDependency> ScopeDependencies { get; set; } = new List<ScopeDependency>();

    public virtual ICollection<ScopeItem> ScopeItems { get; set; } = new List<ScopeItem>();
}
