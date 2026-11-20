using BuildWise.Data;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public class QuotationDocumentViewModel
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string? RequestReference { get; set; }
    public string? LogoUrl { get; set; }
    public QuotationRevision Revision { get; set; } = null!;
    public List<QuotationRevision> Revisions { get; set; } = [];
    public string Version => (Revision.LastUpdate ?? Revision.CreationDate).Ticks.ToString();
    public bool IsLatest => Revisions.Count > 0 && Revisions[0].Id == Revision.Id;
}
