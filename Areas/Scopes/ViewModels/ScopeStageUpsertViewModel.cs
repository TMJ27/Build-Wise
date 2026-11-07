using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeStageUpsertViewModel
{
    public Guid? Id { get; set; }
    public Guid ScopeItemId { get; set; }

    [Required(ErrorMessage = "Enter the stage name."), StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }
}
