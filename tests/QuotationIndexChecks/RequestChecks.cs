using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Requests.Pages;
using BuildWise.Web.Areas.Requests.ViewModels;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

internal static class RequestChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "request@example.test", PasswordHash = "test" };
        var configuration = new ConfigurationBuilder().Build();
        var client = new Client { Id = Guid.NewGuid(), Name = "Zulu", Email = "customer@example.test", Address = "Harare", IsActive = true, Creator = user };
        var inactive = new Client { Id = Guid.NewGuid(), Name = "Inactive", Email = "inactive@example.test", Address = "Harare", IsActive = false, Creator = user };
        db.AddRange(user, client, inactive);
        await db.SaveChangesAsync();
        var empty = new IndexModel(db);
        await empty.OnGetAsync();
        Check(empty.TotalCount == 0 && empty.Records.Count == 0, "Request register must not show sample data.");
        RequestInputViewModel Input() => new()
        {
            ClientId = client.Id, Title = " Paint house ", Description = " Paint the interior walls ",
            SiteAddress = " Bulawayo ", PropertyType = " Existing house "
        };
        CreateModel Form(Guid? userId) => new(db, new CurrentUser(userId))
        {
            Input = Input(), TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        var create = Form(user.Id);
        Check(await create.OnGetAsync(client.Id, null) is PageResult && create.Customers.Count == 1
            && create.Input.SiteAddress == client.Address, "Create must load active customers and GUID preselection.");
        create.Input = Input();
        Check(await create.OnPostAsync() is RedirectToPageResult { PageName: "./Details" }, "Create must save and redirect to database details.");
        var request = await db.ClientRequests.SingleAsync();
        Check(request.Id != Guid.Empty && request.ClientId == client.Id && request.CreatorId == user.Id
            && request.Title == "Paint house" && request.SiteAddress == "Bulawayo" && request.Description == "Paint the interior walls"
            && request.PropertyType == "Existing house" && request.StatusId == (int)ClientRequestStatus.Open
            && request.CreationDate.Kind == DateTimeKind.Local
            && request.ReferenceNumber == $"REQ-{request.CreationDate:yyyyMMdd}-0001",
            "Create must trim fields, generate a date-based reference and save real IDs and audit data.");
        var details = new DetailsModel(db);
        Check(await details.OnGetAsync(request.Id) is PageResult && details.RequestDetails.ScopeItemCount == 0
            && details.RequestDetails.CustomerName == client.Name && details.RequestDetails.CreatorName == user.Email
            && details.CanPrepareScope, "Request details must load customer, creator and scope count.");
        var customerDraft = new ClientRequest
        {
            Id = Guid.NewGuid(), Client = client, Creator = user, Title = "Private draft", SiteAddress = "Harare",
            StatusId = (int)ClientRequestStatus.Draft, CreationDate = DateTime.Now
        };
        db.ClientRequests.Add(customerDraft);
        await db.SaveChangesAsync();
        Check(await new DetailsModel(db).OnGetAsync(customerDraft.Id) is NotFoundResult,
            "Staff cannot open a customer request before the customer submits it.");
        var scopes = new BuildWise.Web.Areas.Scopes.Pages.IndexModel(db);
        await scopes.OnGetAsync();
        Check(scopes.Requests.Single().RequestId == request.Id && scopes.Requests.Single().ItemCount == 0,
            "Only submitted requests appear in Scope; customer drafts stay private.");

        foreach (var id in new Guid?[] { null, Guid.Empty, Guid.NewGuid(), inactive.Id })
        {
            var invalid = Form(user.Id);
            invalid.Input.ClientId = id;
            Check(await invalid.OnPostAsync() is PageResult && !invalid.ModelState.IsValid && invalid.Customers.Count == 1,
                "Missing, unknown and inactive customers must fail and reload options.");
        }
        var invalidText = Form(user.Id);
        invalidText.Input.Title = " ";
        invalidText.Input.SiteAddress = new string('x', 51);
        Check(await invalidText.OnPostAsync() is PageResult && !invalidText.ModelState.IsValid, "Database field limits must be enforced.");
        Check(await Form(null).OnPostAsync() is UnauthorizedResult, "Creating a request requires authentication.");
        Check(await Form(user.Id).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(Guid.NewGuid()) is NotFoundResult, "Unknown customers and requests must return 404.");

        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Painting", IsActive = true };
        var service = new Service { Id = Guid.NewGuid(), Name = "Painting", IsActive = true, Category = category, Creator = user };
        db.AddRange(category, service);
        await db.SaveChangesAsync();
        var scopeForm = new BuildWise.Web.Areas.Scopes.Pages.Items.UpsertModel(db, new CurrentUser(user.Id))
        {
            Input = new() { RequestId = request.Id, ServiceId = service.Id, Title = "Paint rooms", Quantity = 3 },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        Check(await scopeForm.OnPostAsync(null, request.Id) is RedirectToPageResult, "A created request must accept real scope items.");
        await details.OnGetAsync(request.Id);
        Check(details.RequestDetails.ScopeItemCount == 1, "Details must reflect saved scope items.");
        var quoteForm = new BuildWise.Web.Areas.Quotations.Pages.UpsertModel(db, new CurrentUser(user.Id),
            new GeminiQuotationWordingService(new HttpClient(), configuration, NullLogger<GeminiQuotationWordingService>.Instance),
            configuration, NullLogger<BuildWise.Web.Areas.Quotations.Pages.UpsertModel>.Instance);
        Check(await quoteForm.OnGetAsync(null, customerDraft.Id) is NotFoundResult,
            "Staff cannot start a quotation from an unsubmitted customer draft.");
        Check(await quoteForm.OnGetAsync(null, request.Id) is PageResult && quoteForm.Input.Items.Single().Title == "Paint rooms",
            "Created request scope must load into quotation creation.");
        var quote = new Quotation { Id = Guid.NewGuid(), Request = request, Creator = user, QuotationNumber = "QT-REQUEST-TEST" };
        foreach (var number in new[] { 1, 2 })
        {
            db.QuotationRevisions.Add(new QuotationRevision
            {
                Id = Guid.NewGuid(), Quotation = quote, RevisionNumber = number, Title = "Work", CompanyNameSnapshot = "BuildWise",
                CustomerNameSnapshot = client.Name, SiteAddressSnapshot = request.SiteAddress, RequestDescriptionSnapshot = "Work",
                Creator = user, StatusId = (int)(number == 1 ? QuotationStatus.Approved : QuotationStatus.Draft),
                IncludesPrices = true, CurrencyCode = "USD", Total = number * 100m
            });
        }
        await db.SaveChangesAsync();
        await details.OnGetAsync(request.Id);
        Check(details.Quotations.Single().RevisionNumber == 2 && details.Quotations.Single().Total == 200m
            && details.Quotations.Single().Status == QuotationStatus.Draft, "Request details must use the latest quotation revision.");
        for (var i = 0; i < 14; i++)
        {
            db.ClientRequests.Add(new ClientRequest
            {
                Id = Guid.NewGuid(), Client = client, Title = $"Extra {i:D2}", SiteAddress = "Harare", Creator = user,
                StatusId = i % 2 == 0 ? (int)ClientRequestStatus.Closed : (int)ClientRequestStatus.Open,
                CreationDate = request.CreationDate.AddDays(i + 1)
            });
        }
        await db.SaveChangesAsync();
        var filtered = new IndexModel(db) { Search = "  BULAWAYO " };
        await filtered.OnGetAsync();
        Check(filtered.Records.Single().Id == request.Id && filtered.TotalCount == 15 && filtered.Count(ClientRequestStatus.Closed) == 7,
            "Search must be trimmed and case-insensitive, while summary counts ignore customer drafts.");
        Check(filtered.Count(ClientRequestStatus.Draft) == 0, "The staff request register must not expose customer drafts.");
        var status = new IndexModel(db) { Status = ClientRequestStatus.Closed };
        await status.OnGetAsync();
        Check(status.FilteredCount == 7 && status.Records.All(row => row.StatusId == (int)ClientRequestStatus.Closed), "Status filtering must use stored enum values.");
        var page = new IndexModel(db) { PageNumber = int.MaxValue, Sort = "oldest" };
        await page.OnGetAsync();
        Check(page.PageNumber == 2 && page.Records.Count == 5 && page.FilteredCount == 15, "Paging must clamp to available database pages.");
        var invalidFilter = new IndexModel(db) { Status = (ClientRequestStatus)999, Sort = "invalid", PageNumber = -1 };
        await invalidFilter.OnGetAsync();
        Check(invalidFilter.Status == null && invalidFilter.Sort == "newest" && invalidFilter.PageNumber == 1, "Unknown filters must normalize safely.");
        request.StatusId = (int)ClientRequestStatus.Closed;
        await db.SaveChangesAsync();
        await details.OnGetAsync(request.Id);
        Check(!details.CanPrepareScope, "Closed requests must not offer scope preparation or new quotations.");
        Console.WriteLine("PASS: database requests, customer validation, field limits, references, filters/paging, details and request-to-scope-to-quotation handoff.");
    }

    private sealed class CurrentUser(Guid? id) : ICurrentUserService
    {
        public Task<Guid?> GetUserIdAsync() => Task.FromResult(id);
        public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    }
    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
