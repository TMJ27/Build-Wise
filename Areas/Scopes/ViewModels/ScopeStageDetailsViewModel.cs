namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeStageDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid ScopeItemId { get; set; }
    public Guid RequestId { get; set; }
    public string ScopeItemTitle { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public bool CanEdit { get; set; }
}
