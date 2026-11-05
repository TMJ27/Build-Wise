using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class UserGroup
{
    [NotMapped]
    public int UserGroupSequence { get; set; }

    [NotMapped]
    public string UserGroupNumber => $"UG-{UserGroupSequence:D4}";
}
