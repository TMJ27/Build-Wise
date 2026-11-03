using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ScopeItem
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public Guid ServiceId { get; set; }

    public string ServiceCodeSnapshot { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public string? UnitCodeSnapshot { get; set; }

    public bool RequireAssessment { get; set; }

    public string? AssessmentNotesJson { get; set; }

    public int? SourceId { get; set; }

    public int ReviewStatusId { get; set; }

    public Guid ReviewedByUserId { get; set; }

    public DateTime? ReviewedOn { get; set; }

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual ClientRequest Request { get; set; } = null!;

    public virtual User ReviewedByUser { get; set; } = null!;

    public virtual ICollection<ScopeClause> ScopeClauses { get; set; } = new List<ScopeClause>();

    public virtual ICollection<ScopeDependency> ScopeDependencyDependsOnScopeItems { get; set; } = new List<ScopeDependency>();

    public virtual ICollection<ScopeDependency> ScopeDependencyScopeItems { get; set; } = new List<ScopeDependency>();

    public virtual ICollection<ScopeStage> ScopeStages { get; set; } = new List<ScopeStage>();

    public virtual Service Service { get; set; } = null!;
}
