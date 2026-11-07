using System.Globalization;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Services.Scopes;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Pages;

public class IndexModel(BuildWiseDbContext db, TimeProvider? timeProvider = null) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Range { get; set; } = "7";
    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "requests";
    public DateOnly Today => DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().ToOffset(TimeSpan.FromHours(2)).DateTime);
    public IReadOnlyList<OverviewRequest> Requests { get; private set; } = [];
    public IReadOnlyList<OverviewQuotation> Quotations { get; private set; } = [];
    public IReadOnlyList<OverviewRequest> VisibleRequests => Requests;
    public IReadOnlyList<OverviewQuotation> VisibleQuotations => Quotations;
    public IReadOnlyList<OverviewCategory> Categories { get; private set; } = [];
    public IReadOnlyList<OverviewCategory> ChartCategories => Categories.Count <= 5 ? Categories
        : Categories.Take(5).Append(new OverviewCategory("Other", Categories.Skip(5).Sum(category => category.Count), 5)).ToArray();
    public IReadOnlyList<DashboardDay> Days { get; private set; } = [];
    public IReadOnlyList<OverviewAttention> Attention { get; private set; } = [];
    public IReadOnlyList<OverviewActivity> Activity { get; private set; } = [];
    public int OpenRequests => Requests.Count(request => ScopeQueries.CanEdit(request.StatusId));
    public int DraftCount => Quotations.Count(quotation => quotation.Status == QuotationStatus.Draft);
    public int ApprovedCount => Quotations.Count(quotation => quotation.Status == QuotationStatus.Approved);
    public int CustomerCount { get; private set; }
    public string ApprovedValueText { get; private set; } = "No priced approvals";
    public string RangeCaption => Range == "all" ? "All time" : $"Last {Range} days";
    public string PeriodCaption => Range == "all" ? $"Through {Date(Today)}"
        : $"{Date(Today.AddDays(1 - int.Parse(Range, CultureInfo.InvariantCulture)))} – {Date(Today)}";
    private Dictionary<Guid, OverviewQuotation> quotationByRequest = [];

    public async Task<IActionResult> OnGetRecordsAsync()
    {
        await OnGetAsync();
        Response.Headers["X-Dashboard-Records"] = "true";
        return Partial("_DashboardRecords", this);
    }

    public async Task OnGetAsync()
    {
        Range = Range is "7" or "30" or "all" ? Range : "7";
        Tab = Tab == "quotations" ? Tab : "requests";
        ModelState.Remove(nameof(Range));
        ModelState.Remove(nameof(Tab));
        // Harare uses UTC+02:00. Apply local calendar boundaries to timestamps stored in UTC.
        var end = Today.AddDays(1).ToDateTime(TimeOnly.MinValue).AddHours(-2);
        var start = Range == "all" ? (DateTime?)null
            : Today.AddDays(1 - int.Parse(Range, CultureInfo.InvariantCulture)).ToDateTime(TimeOnly.MinValue).AddHours(-2);
        var requests = db.ClientRequests.AsNoTracking().Where(request => request.CreationDate < end);
        if (start.HasValue) requests = requests.Where(request => request.CreationDate >= start.Value);
        Requests = await requests.OrderByDescending(request => request.CreationDate).ThenBy(request => request.Id)
            .Select(request => new OverviewRequest
            {
                Id = request.Id, Title = request.Title, Customer = request.Client.Name, ReferenceNumber = request.ReferenceNumber,
                StatusId = request.StatusId, CreationDate = request.CreationDate, ScopeItemCount = request.ScopeItems.Count,
                PendingScopeCount = request.ScopeItems.Count(item => item.ReviewStatusId != (int)ScopeReviewStatus.Reviewed)
            }).ToListAsync();
        CustomerCount = await db.Clients.CountAsync();
        var allQuotations = await db.Quotations.AsNoTracking().Select(quotation => new
        {
            Quotation = quotation,
            Revision = quotation.QuotationRevisions.OrderByDescending(revision => revision.RevisionNumber).FirstOrDefault()
        }).Select(row => new OverviewQuotation
        {
            Id = row.Quotation.Id, RequestId = row.Quotation.RequestId, QuotationNumber = row.Quotation.QuotationNumber,
            Title = row.Revision == null ? row.Quotation.Request.Title : row.Revision.Title,
            Customer = row.Revision == null ? row.Quotation.Request.Client.Name : row.Revision.CustomerNameSnapshot,
            Status = row.Revision == null ? null : (QuotationStatus?)row.Revision.StatusId,
            CreationDate = row.Revision == null ? null : row.Revision.CreationDate,
            ApprovalDate = row.Revision == null ? null : row.Revision.ApprovalDate,
            IncludesPrices = row.Revision != null && row.Revision.IncludesPrices,
            CurrencyCode = row.Revision == null ? null : row.Revision.CurrencyCode,
            Total = row.Revision == null ? null : row.Revision.Total
        }).ToListAsync();
        Quotations = allQuotations.Where(quotation => quotation.CreationDate < end
                && (!start.HasValue || quotation.CreationDate >= start.Value))
            .OrderByDescending(quotation => quotation.CreationDate).ThenBy(quotation => quotation.Id).ToArray();
        quotationByRequest = allQuotations.GroupBy(quotation => quotation.RequestId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(quotation => quotation.CreationDate).ThenBy(quotation => quotation.Id).First());
        var values = Quotations.Where(quotation => quotation.Status == QuotationStatus.Approved && quotation.IncludesPrices && quotation.Total.HasValue)
            .GroupBy(quotation => string.IsNullOrWhiteSpace(quotation.CurrencyCode) ? "Currency not set" : quotation.CurrencyCode.Trim().ToUpperInvariant())
            .OrderBy(group => group.Key).Select(group => $"{group.Key} {group.Sum(quotation => quotation.Total!.Value).ToString("N2", CultureInfo.InvariantCulture)}").ToArray();
        ApprovedValueText = values.Length == 0 ? "No priced approvals" : string.Join(" · ", values);

        // Each request belongs to one chart bucket so multi-service requests are counted once.
        var categoryQuery = db.ScopeItems.AsNoTracking().Where(item => item.Request.CreationDate < end);
        if (start.HasValue) categoryQuery = categoryQuery.Where(item => item.Request.CreationDate >= start.Value);
        var links = await categoryQuery.Select(item => new { item.RequestId, item.Service.CategoryId, item.Service.Category.Name }).Distinct().ToListAsync();
        var categoryLookup = links.ToLookup(link => link.RequestId);
        Categories = Requests.Select(request =>
        {
            var categories = categoryLookup[request.Id].ToArray();
            return categories.Length == 0 ? "Scope not prepared" : categories.Length == 1 ? categories[0].Name : "Multiple categories";
        }).GroupBy(name => name).OrderByDescending(group => group.Count()).ThenBy(group => group.Key)
            .Select((group, index) => new OverviewCategory(group.Key, group.Count(), index % 5)).ToArray();

        var chartStart = Today.AddDays(-6).ToDateTime(TimeOnly.MinValue).AddHours(-2);
        var revisionEvents = await db.QuotationRevisions.AsNoTracking()
            .Where(revision => (revision.CreationDate >= chartStart && revision.CreationDate < end)
                || (revision.ApprovalDate >= chartStart && revision.ApprovalDate < end))
            .Select(revision => new { revision.CreationDate, revision.ApprovalDate }).ToListAsync();
        Days = Enumerable.Range(0, 7).Select(offset => Today.AddDays(offset - 6)).Select(date => new DashboardDay(date,
            revisionEvents.Count(revision => LocalDate(revision.CreationDate) == date),
            revisionEvents.Count(revision => revision.ApprovalDate.HasValue && LocalDate(revision.ApprovalDate.Value) == date))).ToArray();

        Attention = Requests.Where(request => ScopeQueries.CanEdit(request.StatusId)).Select(request =>
        {
            if (request.StatusId == (int)ClientRequestStatus.Pending_Verification)
                return new OverviewAttention("Confirm request details", "orange", "clarify", request);
            if (request.ScopeItemCount == 0) return new OverviewAttention("Prepare the scope", "violet", "scope", request);
            if (request.PendingScopeCount > 0) return new OverviewAttention("Review scope items", "violet", "scope", request);
            if (!quotationByRequest.TryGetValue(request.Id, out var quote)) return new OverviewAttention("Prepare a quotation", "", "quote-edit", request);
            return quote.Status == QuotationStatus.Draft ? new OverviewAttention("Review draft quotation", "", "quote-edit", request) : null;
        }).Where(item => item is not null).Take(3).Cast<OverviewAttention>().ToArray();
        Activity = Requests.Select(request => new OverviewActivity("Request captured", $"{request.Customer} · {request.Title}", $"request/{request.Id}", request.CreationDate))
            .Concat(Quotations.Select(quotation => new OverviewActivity("Quotation prepared", $"{quotation.QuotationNumber} · {Amount(quotation)}", $"quote/{quotation.Id}", quotation.CreationDate!.Value)))
            .Concat(allQuotations.Where(quotation => quotation.Status == QuotationStatus.Approved && quotation.ApprovalDate < end
                    && (!start.HasValue || quotation.ApprovalDate >= start.Value) && quotation.ApprovalDate.HasValue)
                .Select(quotation => new OverviewActivity("Quotation approved", quotation.QuotationNumber, $"quote/{quotation.Id}", quotation.ApprovalDate!.Value)))
            .OrderByDescending(activity => activity.Timestamp).Take(4).ToArray();
    }

    public string ScreenUrl(string route)
    {
        var parts = route.Split('/');
        var validId = Guid.TryParse(parts.ElementAtOrDefault(1), out var id);
        return parts[0] switch
        {
            "requests" => Url.Page("/Index", new { area = "Requests" })!,
            "request" or "clarify" when validId => Url.Page("/Details", new { area = "Requests", id })!,
            "scope" when validId => Url.Page("/Details", new { area = "Scopes", id })!,
            "quotations" => Url.Page("/Index", new { area = "Quotations" })!,
            "quote" when validId => Url.Page("/Upsert", new { area = "Quotations", id })!,
            "quote-edit" when validId => quotationByRequest.TryGetValue(id, out var quote)
                ? Url.Page("/Upsert", new { area = "Quotations", id = quote.Id })!
                : Url.Page("/Upsert", new { area = "Quotations", requestId = id })!,
            _ => Url.Page("/Index", new { area = "" })!
        };
    }

    private static DateOnly LocalDate(DateTime date) => DateOnly.FromDateTime(date.AddHours(2));
    public static string Amount(OverviewQuotation quotation) => !quotation.IncludesPrices ? "Not priced"
        : !quotation.Total.HasValue ? "Not set" : $"{quotation.CurrencyCode ?? "Currency not set"} {quotation.Total.Value.ToString("N2", CultureInfo.InvariantCulture)}";
    // Retained for older sample quotation pages that still call this formatter.
    public static string Money(decimal amount) => amount.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    public static string Date(DateOnly date, string format = "dd MMM yyyy") => date.ToString(format, CultureInfo.GetCultureInfo("en-GB"));
}
