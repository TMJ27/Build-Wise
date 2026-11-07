using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Overview = BuildWise.Web.Pages.IndexModel;

internal static class OverviewChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var clock = new Clock(new DateTimeOffset(2026, 11, 7, 22, 30, 0, TimeSpan.Zero));
        var page = new Overview(db, clock);
        await page.OnGetAsync();
        Check(page.Today == new DateOnly(2026, 11, 8) && page.Requests.Count == 0 && page.Quotations.Count == 0
            && page.CustomerCount == 0 && page.Days.Count == 7 && page.Days.All(day => day.Prepared == 0 && day.Approved == 0),
            "Empty Overview must show zeros and use Harare calendar dates, not sample dates.");
        var user = new User { Id = Guid.NewGuid(), Email = "overview@example.test", PasswordHash = "test" };
        var customer = new Client { Id = Guid.NewGuid(), Name = "Customer", Email = "customer@example.test", Address = "Harare", Creator = user };
        db.AddRange(user, customer);
        var boundary = new DateTime(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);
        var today = new DateTime(2026, 11, 7, 22, 15, 0, DateTimeKind.Utc);
        ClientRequest Request(string title, DateTime date, ClientRequestStatus status = ClientRequestStatus.Open)
        {
            var request = new ClientRequest
            {
                Id = Guid.NewGuid(), Client = customer, Creator = user, Title = title, SiteAddress = "Harare",
                CreationDate = date, StatusId = (int)status
            };
            db.ClientRequests.Add(request);
            return request;
        }
        var emptyScope = Request("Needs scope", boundary);
        var multiple = Request("Several categories", today);
        var closed = Request("Closed", today, ClientRequestStatus.Closed);
        var pending = Request("Pending", today, ClientRequestStatus.Pending_Verification);
        var older = Request("Older request", boundary.AddTicks(-1));
        var future = Request("Tomorrow", new DateTime(2026, 11, 8, 22, 0, 0, DateTimeKind.Utc));
        foreach (var name in new[] { "Painting", "Electrical" })
        {
            var category = new ServiceCategory { Id = Guid.NewGuid(), Name = name };
            var service = new Service { Id = Guid.NewGuid(), Name = name, Category = category, Creator = user };
            db.ScopeItems.Add(new ScopeItem
            {
                Id = Guid.NewGuid(), Request = multiple, Service = service, Title = name, ServiceCodeSnapshot = name,
                ReviewedByUser = user, ReviewStatusId = (int)ScopeReviewStatus.Reviewed
            });
        }
        Quotation Quote(string number, ClientRequest request)
        {
            var quote = new Quotation { Id = Guid.NewGuid(), QuotationNumber = number, Request = request, Creator = user };
            db.Quotations.Add(quote);
            return quote;
        }
        QuotationRevision Revision(Quotation quote, int number, QuotationStatus status, DateTime date, string currency, decimal? total, bool priced = true)
        {
            var revision = new QuotationRevision
            {
                Id = Guid.NewGuid(), Quotation = quote, RevisionNumber = number, StatusId = (int)status,
                Title = quote.QuotationNumber, CustomerNameSnapshot = "Snapshot customer", CompanyNameSnapshot = "BuildWise",
                SiteAddressSnapshot = "Harare", RequestDescriptionSnapshot = "Work", Creator = user,
                CreationDate = date, ApprovalDate = status == QuotationStatus.Approved ? today : null,
                IncludesPrices = priced, CurrencyCode = currency, Total = total
            };
            db.QuotationRevisions.Add(revision);
            return revision;
        }
        var draft = Quote("QT-DRAFT", multiple);
        Revision(draft, 1, QuotationStatus.Approved, today.AddDays(-1), "USD", 999m);
        Revision(draft, 2, QuotationStatus.Draft, today, "USD", 300m);
        Revision(Quote("QT-USD", older), 1, QuotationStatus.Approved, today, "USD", 100m);
        Revision(Quote("QT-ZAR", older), 1, QuotationStatus.Approved, today, "ZAR", 200m);
        Revision(Quote("QT-UNPRICED", older), 1, QuotationStatus.Approved, today, "USD", 800m, false);
        Revision(Quote("QT-MISSING", older), 1, QuotationStatus.Approved, today, "USD", null);
        Revision(Quote("QT-OLD", older), 1, QuotationStatus.Approved, boundary.AddDays(-2), "USD", 50m);
        Quote("QT-NO-REVISION", older);
        await db.SaveChangesAsync();
        await page.OnGetAsync();
        Check(page.Requests.Count == 4 && page.Requests.Any(r => r.Id == emptyScope.Id)
            && page.Requests.All(r => r.Id != older.Id && r.Id != future.Id), "Seven-day range must respect inclusive local start and exclusive local end.");
        Check(page.CustomerCount == 1 && page.OpenRequests == 3 && page.DraftCount == 1 && page.ApprovedCount == 4,
            "Summary counts must use stored statuses and only latest quotation revisions.");
        Check(page.ApprovedValueText == "USD 100.00 · ZAR 200.00", "Approved totals must stay separate by currency and exclude unpriced/missing amounts.");
        Check(page.Categories.Sum(category => category.Count) == page.Requests.Count
            && page.Categories.Single(category => category.Name == "Multiple categories").Count == 1,
            "Category chart must count each request once, including requests with several categories.");
        Check(page.Days.Single(day => day.Date == page.Today).Approved == 6,
            "Approval chart must use actual approval dates, including historical revisions and older prepared quotations.");
        Check(page.Attention.Count == 3 && page.Attention.All(a => a.Request.Id != closed.Id)
            && page.Attention.Any(a => a.Request.Id == multiple.Id && a.Route == "quote-edit"), "Attention must offer actions for unfinished requests only.");
        Check(page.VisibleQuotations.Any(q => q.RequestId == older.Id && q.Customer == "Snapshot customer"),
            "Recent quotations must work when their request is outside the date range.");
        Check(Overview.Amount(new OverviewQuotation { IncludesPrices = false }) == "Not priced"
            && Overview.Amount(new OverviewQuotation { IncludesPrices = true, Total = 0, CurrencyCode = "USD" }) == "USD 0.00",
            "Quotation values must distinguish unpriced and zero-priced records.");
        page.Url = new TestUrls();
        Check(page.ScreenUrl($"request/{emptyScope.Id}").Contains($"Requests/Details?id={emptyScope.Id}")
            && page.ScreenUrl($"scope/{multiple.Id}").Contains($"Scopes/Details?id={multiple.Id}")
            && page.ScreenUrl($"quote-edit/{multiple.Id}").Contains($"Quotations/Upsert?id={draft.Id}"),
            "Dashboard links must target real GUID routes and the database quotation editor.");
        var month = new Overview(db, clock) { Range = "30", Tab = "quotations" };
        await month.OnGetAsync();
        Check(month.Requests.Count == 5 && month.VisibleRequests.Count == 5 && month.ApprovedCount == 5
            && month.ApprovedValueText == "USD 150.00 · ZAR 200.00", "Date selection must update recent records and latest-revision figures together.");
        var invalid = new Overview(db, clock) { Range = "invalid", Tab = "invalid" };
        await invalid.OnGetAsync();
        Check(invalid.Range == "7" && invalid.Tab == "requests", "Unknown dashboard filters must normalize.");
        var all = new Overview(db, clock) { Range = "all" };
        await all.OnGetAsync();
        Check(all.Requests.Count == 5 && all.Requests.All(r => r.Id != future.Id), "All-time overview must exclude future calendar dates.");
        page.MetadataProvider = new EmptyModelMetadataProvider();
        page.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext(),
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        Check(await page.OnGetRecordsAsync() is PartialViewResult { ViewName: "_DashboardRecords" }
            && page.Response.Headers["X-Dashboard-Records"] == "true", "Async tabs must return the expected partial and identifying header.");
        for (var i = 0; i < 6; i++)
        {
            var request = Request($"Category request {i}", today);
            var category = new ServiceCategory { Id = Guid.NewGuid(), Name = $"Category {i}" };
            var service = new Service { Id = Guid.NewGuid(), Name = category.Name, Category = category, Creator = user };
            db.ScopeItems.Add(new ScopeItem
            {
                Id = Guid.NewGuid(), Request = request, Service = service, Title = service.Name,
                ServiceCodeSnapshot = $"CAT-{i}", ReviewedByUser = user
            });
        }
        await db.SaveChangesAsync();
        await page.OnGetAsync();
        Check(page.ChartCategories.Count == 6 && page.ChartCategories.Last().Name == "Other"
            && page.ChartCategories.Sum(category => category.Count) == page.Requests.Count,
            "Top-five chart grouping must preserve the total request count.");
        Check(page.Activity.Select(activity => activity.Timestamp).SequenceEqual(page.Activity.Select(activity => activity.Timestamp).OrderDescending()),
            "Workspace activity must use timestamps to order events within the same day.");
        Console.WriteLine("PASS: database Overview, local dates/ranges, latest revisions, currency totals, category counts, approval dates, attention and GUID navigation.");
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestUrls : IUrlHelper
    {
        public ActionContext ActionContext { get; } = new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        public string? Action(UrlActionContext context) => throw new NotSupportedException();
        public string? Content(string? path) => path;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? name, object? values) => throw new NotSupportedException();
        public string? RouteUrl(UrlRouteContext context)
        {
            var values = new RouteValueDictionary(context.Values);
            return $"/{values["area"]}{values["page"]}?id={values.GetValueOrDefault("id")}&requestId={values.GetValueOrDefault("requestId")}";
        }
    }
}
