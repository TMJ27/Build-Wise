using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Client
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string Address { get; set; } = null!;

    public int ClientTypeId { get; set; }

    public string? NotesJson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual ICollection<ClientRequest> ClientRequests { get; set; } = new List<ClientRequest>();

    public virtual User Creator { get; set; } = null!;
}
