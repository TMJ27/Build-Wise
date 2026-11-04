using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Services.Pages;

public class IndexModel(CatalogueWorkspace catalogue) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Category { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "active";

    public IReadOnlyList<CatalogueService> All { get; private set; } = [];
    public IReadOnlyList<CatalogueService> Records { get; private set; } = [];
    public int ActiveCount => All.Count(service => service.IsActive);
    public int ArchivedCount => All.Count(service => !service.IsActive);
    public int AssessmentCount => All.Count(service => service.IsActive && service.RequiresSiteAssessment);
    public int CategoryCount => All.Where(service => service.IsActive).Select(service => service.Category).Distinct().Count();

    public void OnGet()
    {
        Search = (Search ?? string.Empty).Trim();

        if (Search.Length > 120)
        {
            Search = Search[..120];
        }

        Status = Status is "all" or "archived" ? Status : "active";
        Category = CatalogueWorkspace.Categories.Contains(Category) ? Category : "all";
        All = catalogue.All;
        Records = All.Where(service =>
            (Status == "all" || service.IsActive == (Status == "active")) &&
            (Category == "all" || service.Category == Category) &&
            $"{service.Code} {service.Name} {service.Description} {service.Category}"
                .Contains(Search, StringComparison.OrdinalIgnoreCase)).ToArray();

        ModelState.Clear();
    }
}
