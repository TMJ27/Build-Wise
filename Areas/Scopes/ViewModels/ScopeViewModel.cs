namespace BuildWise.Web.Areas.Scopes.ViewModels;

// A presentation of ClientRequest and its ScopeItems, not a separate Scope entity.
public sealed class ScopeViewModel
{
    public Guid RequestId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? SiteAddress { get; set; }
    public string? Description { get; set; }
    public int ItemCount { get; set; }
    public int AssessmentCount { get; set; }
    public int StatusId { get; set; }
    public bool CanEdit => BuildWise.Web.Services.Scopes.ScopeQueries.CanEdit(StatusId);
}
