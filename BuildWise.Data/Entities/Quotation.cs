using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Quotation
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public string QuotationNumber { get; set; } = null!;

    public Guid CreatorId { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual ICollection<QuotationRevision> QuotationRevisions { get; set; } = new List<QuotationRevision>();

    public virtual ClientRequest Request { get; set; } = null!;
}
