using BuildWise.Web.Services;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Requests;

public class IndexModel(RequestWorkspace workspace) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Search { get; set; } = "";
    [BindProperty(SupportsGet = true)] public string Status { get; set; } = "all";
    [BindProperty(SupportsGet = true)] public string Category { get; set; } = "all";
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "newest";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public IReadOnlyList<DashboardRequest> Records { get; private set; } = [];
    public IReadOnlyList<DashboardRequest> All { get; private set; } = [];
    public string[] Categories { get; private set; } = [];
    public static readonly string[] Statuses = ["Needs clarification", "Scope review", "Ready to quote", "Draft quotation", "Quotation approved"];
    public int Total { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / 8d));
    public int Count(string status) => status == "all" ? All.Count : All.Count(r => r.Status == status);
    public void OnGet()
    {
        All = workspace.Requests;
        Categories = All.Select(r => r.Category).Distinct().Order().ToArray();
        Search = (Search ?? "").Trim();
        if (Search.Length > 120) Search = Search[..120];
        if (!Statuses.Contains(Status)) Status = "all";
        if (!Categories.Contains(Category)) Category = "all";
        if (Sort is not ("newest" or "oldest" or "customer")) Sort = "newest";
        var filtered = All.Where(r => (Status == "all" || r.Status == Status)
            && (Category == "all" || r.Category == Category)
            && $"{r.Id} {r.Title} {r.Customer} {r.Category}".Contains(Search, StringComparison.OrdinalIgnoreCase));
        Total = filtered.Count();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        Records = (Sort switch {
            "oldest" => filtered.OrderBy(r => r.CreatedOn).ThenBy(r => r.Id),
            "customer" => filtered.OrderBy(r => r.Customer).ThenByDescending(r => r.Id),
            _ => filtered.OrderByDescending(r => r.CreatedOn).ThenByDescending(r => r.Id)
        }).Skip((PageNumber - 1) * 8).Take(8).ToArray();
        ModelState.Clear();
    }
}
