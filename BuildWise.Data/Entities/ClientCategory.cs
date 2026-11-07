using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class ClientCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? LastUpdated { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    public virtual User Creator { get; set; } = null!;
}
