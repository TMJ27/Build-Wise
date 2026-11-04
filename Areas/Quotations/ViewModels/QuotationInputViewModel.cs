using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public sealed class QuotationInputViewModel
{
    [Range(1, int.MaxValue)]
    public int RequestId { get; set; }

    [Required, StringLength(2000)]
    public string Terms { get; set; } = "Valid for 14 days. Work starts after site access and payment arrangements are confirmed.";

    [Required, StringLength(2000)]
    public string Exclusions { get; set; } = "Additional work outside the agreed scope will be quoted separately.";

    public List<LineInputViewModel> Lines { get; set; } = [new()];
}
