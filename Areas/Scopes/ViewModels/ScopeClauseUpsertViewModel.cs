using System.ComponentModel.DataAnnotations;
using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeClauseUpsertViewModel
{
    public Guid? Id { get; set; }
    public Guid ScopeItemId { get; set; }

    [Required(ErrorMessage = "Choose a clause type.")]
    public ScopeClauseType? ClauseType { get; set; } = ScopeClauseType.Inclusion;

    [Required(ErrorMessage = "Enter the clause wording."), StringLength(4000)]
    public string Description { get; set; } = string.Empty;
}
