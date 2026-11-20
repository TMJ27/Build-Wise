namespace BuildWise.Web.Areas.Requests.ViewModels;

public class RequestIndexViewModel
{
    public Guid Id { get; set; }
    public string? ReferenceNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string SiteAddress { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime CreationDate { get; set; }
    public int ScopeItemCount { get; set; }
}
