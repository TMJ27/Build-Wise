namespace BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;

public sealed class InclusionRowViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreationDate { get; set; }
}
