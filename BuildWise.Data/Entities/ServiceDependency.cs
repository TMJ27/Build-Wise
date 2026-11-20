using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ServiceDependency
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }

    public Guid DependsOnServiceId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual Service DependsOnService { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
