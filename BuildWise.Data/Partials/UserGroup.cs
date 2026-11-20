using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class UserGroup
{
    [NotMapped]
    public int UserGroupSequence { get; set; }

    [NotMapped]
    public string UserGroupNumber => GeneratedNumberFormat.Format("UG", CreationDate, UserGroupSequence);
}

