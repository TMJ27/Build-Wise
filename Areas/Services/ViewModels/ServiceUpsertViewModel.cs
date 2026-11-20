using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Services.ViewModels;

public sealed class ServiceUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Enter the service name.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a category.")]
    [Display(Name = "Category")]
    public Guid? CategoryId { get; set; }

    public string? Description { get; set; }

    [Display(Name = "Requires a site assessment")]
    public bool RequireSiteAssessment { get; set; }

    [Display(Name = "Active service")]
    public bool IsActive { get; set; } = true;
}
