using System.ComponentModel.DataAnnotations;
using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class GeminiScopeDraftViewModel
{
    public bool Selected { get; set; }

    [Required]
    public Guid ServiceId { get; set; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [StringLength(50)]
    public string? UnitCode { get; set; }

    public bool RequireAssessment { get; set; }

    public List<GeminiScopeClauseDraftViewModel> Clauses { get; set; } = [];
}

public sealed class GeminiScopeClauseDraftViewModel
{
    [EnumDataType(typeof(ScopeClauseType))]
    public ScopeClauseType Type { get; set; } = ScopeClauseType.Inclusion;

    [StringLength(4000, MinimumLength = 1)]
    public string? Wording { get; set; }
}
