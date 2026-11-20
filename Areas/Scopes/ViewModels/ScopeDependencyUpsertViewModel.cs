using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeDependencyUpsertViewModel
{
    public Guid? Id { get; set; }
    public Guid ScopeItemId { get; set; }

    [Required(ErrorMessage = "Choose the work item that must happen first.")]
    public Guid? DependsOnScopeItemId { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }
}
