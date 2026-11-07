using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;

public sealed class ServiceCategoryUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Enter the category name.")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
