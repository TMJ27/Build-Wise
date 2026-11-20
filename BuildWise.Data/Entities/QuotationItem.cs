using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class QuotationItem
{
    public Guid Id { get; set; }

    public Guid QuotationRevisionId { get; set; }

    public Guid RequestId { get; set; }

    public Guid ScopeItemId { get; set; }

    public Guid ServiceId { get; set; }

    public string ServiceCodeSnapshot { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public string? UnitCodeSnapshot { get; set; }

    public decimal? LineAmount { get; set; }

    public bool RequiresSiteAssessment { get; set; }

    public string? AssessmentNotesSnapshot { get; set; }

    public string? WorkStagesSnapshotJson { get; set; }

    public string? DependenciesSnapshotJson { get; set; }

    public virtual QuotationRevision QuotationRevision { get; set; } = null!;

    public virtual ClientRequest Request { get; set; } = null!;

    public virtual ScopeItem ScopeItem { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
