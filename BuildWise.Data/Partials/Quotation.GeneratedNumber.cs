using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWise.Data;

public partial class Quotation
{
    [NotMapped]
    public int QuotationSequence { get; set; }

    [NotMapped]
    public DateTime QuotationNumberDate { get; set; }

    [NotMapped]
    public string GeneratedQuotationNumber => GeneratedNumberFormat.Format("QT", QuotationNumberDate, QuotationSequence);
}
