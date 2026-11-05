using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Currency
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string Symbol { get; set; } = null!;

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public virtual User Creator { get; set; } = null!;
}
