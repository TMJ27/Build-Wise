using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Unit
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<ServiceUnit> ServiceUnits { get; set; } = new List<ServiceUnit>();
}
