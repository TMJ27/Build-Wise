using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class Service
{
    [NotMapped]
    public int ServiceSequence { get; set; }

    [NotMapped]
    public string ServiceNumber => $"SRV-{ServiceSequence:D4}";
}
