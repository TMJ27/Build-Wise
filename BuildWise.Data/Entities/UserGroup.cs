using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class UserGroup
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int RightsId { get; set; }

    public DateTime CreationDate { get; set; }

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
