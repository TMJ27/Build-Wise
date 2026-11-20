namespace BuildWise.Web.Areas.Configuration.ViewModels.ClientCategories;

public sealed class ClientCategoryIndexViewModel
{
    public Guid Id { get; set; }
    public string CategoryNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ClientCount { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}
