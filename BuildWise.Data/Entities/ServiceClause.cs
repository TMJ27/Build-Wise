using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ServiceClause
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    public int? ClauseTypeId { get; set; }

    public string? Description { get; set; }

    public virtual Service Service { get; set; } = null!;
}
