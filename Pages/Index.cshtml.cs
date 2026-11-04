using System.Globalization;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages;

public class IndexModel(
    BuildWise.Web.Services.RequestWorkspace workspace,
    BuildWise.Web.Services.QuotationWorkspace quotations,
    BuildWise.Web.Services.CatalogueWorkspace catalogue) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Range { get; set; } = "30";
    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "requests";
    [BindProperty(SupportsGet = true)] public string Search { get; set; } = string.Empty;

    public DateOnly Today => DashboardSampleData.Today;
    public IReadOnlyList<DashboardRequest> Requests { get; private set; } = [];
    public IReadOnlyList<DashboardQuotation> Quotations { get; private set; } = [];
    public IReadOnlyList<DashboardRequest> VisibleRequests { get; private set; } = [];
    public IReadOnlyList<DashboardQuotation> VisibleQuotations { get; private set; } = [];
    public IReadOnlyList<DashboardCategory> Categories { get; private set; } = [];
    public IReadOnlyList<DashboardDay> Days { get; private set; } = [];
    public IReadOnlyList<DashboardStage> Stages { get; private set; } = [];
    public IReadOnlyList<DashboardAttention> Attention { get; private set; } = [];
    public IReadOnlyList<DashboardActivity> Activity { get; private set; } = [];
    public int OpenRequests => Requests.Count(r => r.Status is not ("Quotation approved" or "Archived"));
    public int ClarificationCount => Requests.Count(r => r.Status == "Needs clarification");
    public int DraftCount => Quotations.Count(q => q.Status == "Draft");
    public int ApprovedCount => Quotations.Count(q => q.Status == "Approved");
    public int CustomerCount => workspace.CustomerCount;
    public decimal DraftValue => Quotations.Where(q => q.Status == "Draft").Sum(q => q.Amount);
    public decimal ApprovedValue => Quotations.Where(q => q.Status == "Approved").Sum(q => q.Amount);
    public int ActiveServiceCount => catalogue.All.Count(service => service.IsActive);
    public int MaximumDayCount => Math.Max(1, Days.Select(d => d.Prepared).DefaultIfEmpty(0).Max());
    public string RangeCaption => Range == "all" ? "All time" : $"Last {Range} days";

    public void OnGet()
    {
        Range = Range is "7" or "30" or "all" ? Range : "30";
        Tab = Tab == "quotations" ? "quotations" : "requests";
        Search = (Search ?? string.Empty).Trim();
        if (Search.Length > 120) Search = Search[..120];
        // Render the normalised values rather than an invalid query-string value
        // retained by model binding (notably for the range select element).
        ModelState.Remove(nameof(Range));
        ModelState.Remove(nameof(Tab));
        ModelState.Remove(nameof(Search));
        Requests = workspace.Requests.Where(r => InRange(r.CreatedOn))
            .OrderByDescending(r => r.CreatedOn).ThenByDescending(r => r.Id).ToArray();
        Quotations = quotations.All.Select(entry => entry.Quotation).Where(q => InRange(q.PreparedOn))
            .OrderByDescending(q => q.PreparedOn).ThenByDescending(q => q.Id).ToArray();
        VisibleRequests = Requests.Where(r => Matches($"{r.Title} {r.Customer} {r.Id}")).ToArray();
        VisibleQuotations = Quotations.Where(q => Matches($"{q.Id} {RequestFor(q.RequestId).Title} {RequestFor(q.RequestId).Customer}")).ToArray();
        string[] colours = ["#637d9d", "#aa6248", "#839a8e", "#a394b6", "#a5b6c7"];
        Categories = Requests.GroupBy(r => r.Category).OrderByDescending(g => g.Count())
            .Select((g, i) => new DashboardCategory(g.Key, g.Count(), g.Count() * 100 / Math.Max(1, Requests.Count), colours[i % colours.Length])).ToArray();
        Days = Enumerable.Range(0, 7).Select(i => Today.AddDays(i - 6)).Select(date =>
            new DashboardDay(date, Quotations.Count(q => q.PreparedOn == date), Quotations.Count(q => q.PreparedOn == date && q.Status == "Approved"))).ToArray();
        var stages = new[] { ("Clarify", "Needs clarification", "clarify"), ("Review", "Scope review", "scope"),
            ("Prepare", "Ready to quote", "quote-edit"), ("Quote", "Draft quotation", "quote-edit") };
        Stages = stages.Select(s => new DashboardStage(s.Item1, s.Item2, s.Item3,
            Requests.Count(r => r.Status == s.Item2), Requests.FirstOrDefault(r => r.Status == s.Item2)?.Id)).ToArray();
        Attention = Stages.Take(3).Where(s => s.FirstRequestId.HasValue).Select(s => new DashboardAttention(
            s.Name == "Clarify" ? "Confirm the missing details" : s.Name == "Review" ? "Review a proposed scope" : "Prepare the next quotation",
            s.Name == "Clarify" ? "alert" : s.Name == "Review" ? "check" : "file",
            s.Name == "Clarify" ? "orange" : s.Name == "Review" ? "violet" : string.Empty,
            s.Route, Requests.First(r => r.Id == s.FirstRequestId))).ToArray();
        Activity = Requests.Select(r => new DashboardActivity("Request captured", $"{r.Customer} · {r.Title}", "file", $"request/{r.Id}", r.CreatedOn))
            .Concat(Quotations.Select(q => new DashboardActivity(q.Status == "Approved" ? "Quotation approved" : "Quotation drafted",
                $"BW-2026-{q.Id:D4} · {Money(q.Amount)}", q.Status == "Approved" ? "check" : "edit", $"quote/{q.Id}", q.PreparedOn)))
            .OrderByDescending(a => a.Date).Take(4).ToArray();
    }

    public DashboardRequest RequestFor(int id) => workspace.Requests.First(r => r.Id == id);
    public string ScreenUrl(string route)
    {
        var parts = route.Split('/');
        return parts[0] switch
        {
            "requests" => Url.Page("/Index", new { area = "Requests" })!,
            "new-request" => Url.Page("/Create", new { area = "Requests" })!,
            "request" or "clarify" or "scope" => Url.Page("/Details", new { area = "Requests", id = parts.ElementAtOrDefault(1) })!,
            "quote-edit" => QuotationEditorUrl(parts.ElementAtOrDefault(1)),
            "catalogue" => Url.Page("/Index", new { area = "Services" })!,
            "quotations" => Url.Page("/Index", new { area = "Quotations" })!,
            "quote" => Url.Page("/Details", new { area = "Quotations", id = parts.ElementAtOrDefault(1) })!,
            _ => "/"
        };
    }

    private string QuotationEditorUrl(string? requestId)
    {
        var existing = quotations.All.FirstOrDefault(entry => entry.Quotation.RequestId.ToString() == requestId);

        if (existing is not null)
        {
            return existing.Quotation.Status == "Draft"
                ? Url.Page("/Edit", new { area = "Quotations", id = existing.Quotation.Id })!
                : Url.Page("/Details", new { area = "Quotations", id = existing.Quotation.Id })!;
        }

        return Url.Page("/Edit", new { area = "Quotations", requestId })!;
    }
    public static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    public static string Date(DateOnly date, string format = "dd MMM yyyy") => date.ToString(format, CultureInfo.GetCultureInfo("en-GB"));
    private bool Matches(string text) => text.Contains(Search, StringComparison.OrdinalIgnoreCase);
    private bool InRange(DateOnly date) => Range == "all" || date >= Today.AddDays(1 - int.Parse(Range, CultureInfo.InvariantCulture)) && date <= Today;
}
