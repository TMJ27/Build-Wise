using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;

public sealed class InclusionDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public ActiveStatus? Status => IsActive.HasValue
        ? (IsActive.Value ? ActiveStatus.Active : ActiveStatus.Inactive)
        : null;
    public DateTime? CreationDate { get; set; }
    public DateTime? LastUpdated { get; set; }
}
