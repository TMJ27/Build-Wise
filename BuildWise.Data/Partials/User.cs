using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class User
{
    [NotMapped]
    public int UserSequence { get; set; }

    [NotMapped]
    public string UserNumber => $"USR-{UserSequence:D4}";
}
