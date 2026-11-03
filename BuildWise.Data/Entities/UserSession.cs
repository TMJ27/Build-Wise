using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class UserSession
{
    public Guid UserId { get; set; }

    public DateTime LoginDate { get; set; }

    public DateTime? LockoutDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public virtual User User { get; set; } = null!;
}
