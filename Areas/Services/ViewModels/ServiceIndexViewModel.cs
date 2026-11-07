using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Services.ViewModels;

public sealed class ServiceIndexViewModel
{
    public Guid Id { get; set; }
    public string ServiceNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool RequireSiteAssessment { get; set; }
    public bool IsActive { get; set; }
    public ActiveStatus Status => IsActive ? ActiveStatus.Active : ActiveStatus.Inactive;
    public DateTime CreationDate { get; set; }
}
