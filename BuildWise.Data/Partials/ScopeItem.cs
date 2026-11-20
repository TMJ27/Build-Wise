using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class ScopeItem
{
    [NotMapped]
    public int ScopeItemSequence { get; set; }

    [NotMapped]
    public DateTime ScopeItemNumberDate { get; set; }

    [NotMapped]
    public string ScopeItemNumber => GeneratedNumberFormat.Format("SCP", ScopeItemNumberDate, ScopeItemSequence);
}
