using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Requests.ViewModels;

public sealed class RequestInputViewModel
{
    [Required(ErrorMessage = "Choose a customer.")]
    public Guid? ClientId { get; set; }
    [Required, StringLength(50)]
    public string Title { get; set; } = string.Empty;
    [StringLength(50)]
    public string? ReferenceNumber { get; set; }
    [Required, StringLength(50)]
    public string SiteAddress { get; set; } = string.Empty;
    [StringLength(50)]
    public string? PropertyType { get; set; }
    [Required, StringLength(3000)]
    public string Description { get; set; } = string.Empty;
}
