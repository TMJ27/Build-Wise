using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class Currency
{
    [NotMapped]
    public int CurrencySequence { get; set; }

    [NotMapped]
    public string CurrencyNumber => GeneratedNumberFormat.Format("CUR", CreationDate, CurrencySequence);
}

