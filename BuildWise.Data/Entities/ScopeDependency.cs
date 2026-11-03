using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ScopeDependency
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public Guid ScopeItemId { get; set; }

    public Guid DependsOnScopeItemId { get; set; }

    public string? NotesJson { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ScopeItem DependsOnScopeItem { get; set; } = null!;

    public virtual ClientRequest Request { get; set; } = null!;

    public virtual ScopeItem ScopeItem { get; set; } = null!;
}
