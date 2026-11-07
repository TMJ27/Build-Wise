namespace BuildWise.Web.Areas.Requests.ViewModels;

public sealed class RequestDetailsViewModel : RequestIndexViewModel
{
    public string? Description { get; set; }
    public string? PropertyType { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}
