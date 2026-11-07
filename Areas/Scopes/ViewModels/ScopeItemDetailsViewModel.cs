namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeItemDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid? RequestId { get; set; }
    public Guid? ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ServiceName { get; set; }
    public string? ServiceCodeSnapshot { get; set; }
    public int? Quantity { get; set; }
    public string? UnitCodeSnapshot { get; set; }
    public bool? RequireAssessment { get; set; }
    public string? AssessmentNotes { get; set; }
    public string? ReviewStatusName { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedOn { get; set; }
    public bool CanEdit { get; set; }
}
