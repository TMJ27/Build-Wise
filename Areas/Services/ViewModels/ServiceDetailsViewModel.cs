using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Services.ViewModels;

public sealed class ServiceDetailsViewModel
{
    public Guid Id { get; set; }
    public string ServiceNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool CategoryIsActive { get; set; }
    public bool RequireSiteAssessment { get; set; }
    public bool IsActive { get; set; }
    public ActiveStatus Status => IsActive ? ActiveStatus.Active : ActiveStatus.Inactive;
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? LastUpdated { get; set; }
}
