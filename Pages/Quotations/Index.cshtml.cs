using System.Globalization;
using BuildWise.Web.Services;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Quotations;

public class IndexModel(QuotationWorkspace quotations, RequestWorkspace requests) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "newest";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<QuotationEntry> All { get; private set; } = [];
    public IReadOnlyList<QuotationEntry> Records { get; private set; } = [];
    public int Total { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / 8d));
    public int DraftCount => All.Count(entry => entry.Quotation.Status == "Draft");
    public int ApprovedCount => All.Count(entry => entry.Quotation.Status == "Approved");
    public decimal ApprovedValue => All
        .Where(entry => entry.Quotation.Status == "Approved")
        .Sum(entry => entry.Quotation.Amount);

    public DashboardRequest RequestFor(QuotationEntry entry) => requests.Find(entry.Quotation.RequestId)!.Request;

    public static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

    public void OnGet()
    {
        All = quotations.All;
        Search = (Search ?? string.Empty).Trim();

        if (Search.Length > 120)
        {
            Search = Search[..120];
        }

        Status = Status is "Draft" or "Approved" ? Status : "all";
        Sort = Sort is "oldest" or "highest" ? Sort : "newest";

        var filtered = All.Where(entry =>
            (Status == "all" || entry.Quotation.Status == Status) &&
            $"{entry.Number} {RequestFor(entry).Customer} {RequestFor(entry).Title}"
                .Contains(Search, StringComparison.OrdinalIgnoreCase));

        Total = filtered.Count();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);

        var ordered = Sort switch
        {
            "oldest" => filtered.OrderBy(entry => entry.Quotation.PreparedOn).ThenBy(entry => entry.Quotation.Id),
            "highest" => filtered.OrderByDescending(entry => entry.Quotation.Amount).ThenByDescending(entry => entry.Quotation.Id),
            _ => filtered.OrderByDescending(entry => entry.Quotation.PreparedOn).ThenByDescending(entry => entry.Quotation.Id)
        };

        Records = ordered.Skip((PageNumber - 1) * 8).Take(8).ToArray();
        ModelState.Clear();
    }
}
