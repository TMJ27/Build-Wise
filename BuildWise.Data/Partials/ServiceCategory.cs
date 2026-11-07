using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class ServiceCategory
{
    [NotMapped]
    public int CategorySequence { get; set; }

    [NotMapped]
    public string CategoryNumber => $"CAT-{CategorySequence:D4}";
}
