using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class CustomerRequestInputViewModel
{
    [Required, StringLength(50)]
    public string Title { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string SiteAddress { get; set; } = string.Empty;
    [StringLength(50)]
    public string? PropertyType { get; set; }
    [Required, StringLength(3000)]
    public string Description { get; set; } = string.Empty;
}
