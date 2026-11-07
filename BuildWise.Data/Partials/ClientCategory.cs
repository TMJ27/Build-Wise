using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class ClientCategory
{
    [NotMapped]
    public int CategorySequence { get; set; }

    [NotMapped]
    public string CategoryNumber => $"CCAT-{CategorySequence:D4}";
}
