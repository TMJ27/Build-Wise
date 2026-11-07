using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;

public sealed class ServiceCategoryDetailsViewModel
{
    public Guid Id { get; set; }
    public string CategoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public ActiveStatus Status => IsActive ? ActiveStatus.Active : ActiveStatus.Inactive;
    public int ServiceCount { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? LastUpdated { get; set; }
}
