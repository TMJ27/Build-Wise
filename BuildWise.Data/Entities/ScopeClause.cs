using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ScopeClause
{
    public Guid Id { get; set; }

    public Guid ScopeItemId { get; set; }

    public int ClauseTypeId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ScopeItem ScopeItem { get; set; } = null!;
}
