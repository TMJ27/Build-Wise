using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ServiceStage
{
    public Guid Id { get; set; }

    public Guid? ServiceId { get; set; }

    public string? Name { get; set; }

    public string? Desrciption { get; set; }

    public DateTime? CreationDate { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual User? Creator { get; set; }

    public virtual Service? Service { get; set; }
}
