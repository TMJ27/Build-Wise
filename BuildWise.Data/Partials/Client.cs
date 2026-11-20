using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class Client
{
    [NotMapped]
    public int ClientSequence { get; set; }

    [NotMapped]
    public string ClientNumber => GeneratedNumberFormat.Format("CUS", CreationDate, ClientSequence);
}

