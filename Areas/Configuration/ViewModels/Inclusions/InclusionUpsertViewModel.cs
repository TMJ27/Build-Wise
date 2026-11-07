using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;

public sealed class InclusionUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Enter the inclusion name."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the inclusion description."), StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
