using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public class QuotationResponseInputViewModel
{
    public Guid RevisionId { get; set; }
    [Required]
    public string Version { get; set; } = string.Empty;
    public bool Confirm { get; set; }
    [StringLength(2000)]
    public string? DeclineReason { get; set; }
}
