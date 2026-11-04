using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Services.ViewModels;

public sealed class ServiceInputViewModel
{
    [Required, StringLength(30), RegularExpression(@"[A-Za-z0-9][A-Za-z0-9-]*", ErrorMessage = "Use letters, numbers and hyphens for the service code.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Category { get; set; } = string.Empty;

    [Required]
    public string Unit { get; set; } = "Item";

    [Required, StringLength(3000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(10000)]
    public string Inclusions { get; set; } = string.Empty;

    [StringLength(10000)]
    public string? Exclusions { get; set; }

    [StringLength(10000)]
    public string? Questions { get; set; }

    [Required, StringLength(10000)]
    public string Stages { get; set; } = string.Empty;

    public bool RequiresSiteAssessment { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Guid> DependencyIds { get; set; } = [];
}
