using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ServiceCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
