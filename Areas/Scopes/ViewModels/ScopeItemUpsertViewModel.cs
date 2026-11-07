using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Scopes.ViewModels;

public sealed class ScopeItemUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Select a request.")]
    public Guid? RequestId { get; set; }

    [Required(ErrorMessage = "Select a service.")]
    public Guid? ServiceId { get; set; }

    [Required(ErrorMessage = "Enter the scope item title."), StringLength(50)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? Quantity { get; set; }

    [StringLength(50)]
    public string? UnitCodeSnapshot { get; set; }

    public bool RequireAssessment { get; set; }
    public string? AssessmentNotes { get; set; }
    public bool MarkReviewed { get; set; }
}
