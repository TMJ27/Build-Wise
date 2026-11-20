using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeClauseDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid ScopeItemId { get; set; }
    public Guid RequestId { get; set; }
    public string ScopeItemTitle { get; set; } = string.Empty;
    public int ClauseTypeId { get; set; }
    public string ClauseTypeName => Enum.IsDefined(typeof(ScopeClauseType), ClauseTypeId)
        ? ((ScopeClauseType)ClauseTypeId).ToString() : "Unknown";
    public string? Description { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime? CreationDate { get; set; }
    public bool CanEdit { get; set; }
}
