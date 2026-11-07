namespace BuildWise.Web.Areas.Configuration.ViewModels.ClientCategories;

public sealed class ClientCategoryDetailsViewModel
{
    public Guid Id { get; set; }
    public string CategoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClientCount { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public DateTime? LastUpdated { get; set; }
}
