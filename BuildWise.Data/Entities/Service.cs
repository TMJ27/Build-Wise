using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Service
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool RequireSiteAssessment { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public Guid CreatorId { get; set; }

    public virtual ServiceCategory Category { get; set; } = null!;

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual ICollection<ScopeItem> ScopeItems { get; set; } = new List<ScopeItem>();

    public virtual ICollection<ServiceClause> ServiceClauses { get; set; } = new List<ServiceClause>();

    public virtual ICollection<ServiceDependency> ServiceDependencyDependsOnServices { get; set; } = new List<ServiceDependency>();

    public virtual ICollection<ServiceDependency> ServiceDependencyServices { get; set; } = new List<ServiceDependency>();

    public virtual ICollection<ServiceStage> ServiceStages { get; set; } = new List<ServiceStage>();

    public virtual ICollection<ServiceUnit> ServiceUnits { get; set; } = new List<ServiceUnit>();
}
