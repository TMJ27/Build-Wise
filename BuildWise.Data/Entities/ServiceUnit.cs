using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ServiceUnit
{
    public Guid Id { get; set; }

    public Guid? UnitId { get; set; }

    public Guid? ServiceId { get; set; }

    public bool? IsDefault { get; set; }

    public virtual Service? Service { get; set; }

    public virtual Unit? Unit { get; set; }
}
