using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class QuotationApprovalChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "approver@example.test", PasswordHash = "test" };
        var client = new Client { Id = Guid.NewGuid(), Name = "Saved customer", Email = "customer@example.test", Address = "Site", Creator = user };
        var request = new ClientRequest { Id = Guid.NewGuid(), Title = "Conduit", SiteAddress = "Saved site", Client = client, Creator = user, StatusId = (int)ClientRequestStatus.Open, ReferenceNumber = "REQ-20261110-0001" };
        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrical" };
        var service = new Service { Id = Guid.NewGuid(), Name = "Conduit", Creator = user, Category = category };
        var scope = new ScopeItem { Id = Guid.NewGuid(), Request = request, Service = service, Title = "Conduit", ServiceCodeSnapshot = "SRV-20261110-0001", ReviewedByUser = user, ReviewStatusId = (int)ScopeReviewStatus.Reviewed };
        var quote = new Quotation { Id = Guid.NewGuid(), Request = request, Creator = user, QuotationNumber = "legacy number" };
        var revision = new QuotationRevision
        {
            Id = Guid.NewGuid(), Quotation = quote, RevisionNumber = 1, Title = "Saved work", Creator = user,
            CompanyNameSnapshot = "Saved company", CustomerNameSnapshot = "Saved customer", SiteAddressSnapshot = "Saved site",
            RequestDescriptionSnapshot = "Saved description", CreationDate = new DateTime(2026, 11, 10),
            StatusId = (int)QuotationStatus.Draft, IncludesPrices = true, CurrencyCode = "USD", Subtotal = 100, TaxAmount = 0, Total = 100
        };
        var line = new QuotationItem { Id = Guid.NewGuid(), QuotationRevision = revision, Request = request, ScopeItem = scope, Service = service, ServiceCodeSnapshot = scope.ServiceCodeSnapshot, Title = "Conduit", LineAmount = 100 };
        db.AddRange(user, client, request, category, service, scope, quote, revision, line);
        await db.SaveChangesAsync();
        DetailsModel Form(Guid? id) => new(db, new CurrentUser(id))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        var details = Form(user.Id);
        Check(await details.OnGetAsync(quote.Id, null) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
            && details.Document.Number == "QT-20261110-0001", "Details must load a saved GUID quotation with its generated reference.");
        var version = details.Document.Version;
        Check(await Form(null).OnPostApproveAsync(quote.Id, revision.Id, version, true) is UnauthorizedResult, "Approval requires a current user.");
        var unconfirmed = Form(user.Id);
        await unconfirmed.OnPostApproveAsync(quote.Id, revision.Id, version, false);
        Check(!unconfirmed.ModelState.IsValid && revision.StatusId == (int)QuotationStatus.Draft, "Missing confirmation must not approve.");
        var stale = Form(user.Id);
        await stale.OnPostApproveAsync(quote.Id, revision.Id, "old-version", true);
        Check(!stale.ModelState.IsValid && revision.StatusId == (int)QuotationStatus.Draft, "A stale form must not approve.");
        scope.ReviewStatusId = (int)ScopeReviewStatus.Pending;
        await db.SaveChangesAsync();
        var pending = Form(user.Id);
        await pending.OnPostApproveAsync(quote.Id, revision.Id, version, true);
        Check(!pending.ModelState.IsValid, "Pending scope must block approval.");
        scope.ReviewStatusId = (int)ScopeReviewStatus.Reviewed;
        await db.SaveChangesAsync();
        Check(await Form(user.Id).OnPostApproveAsync(quote.Id, revision.Id, version, true) is RedirectToPageResult,
            "A valid draft must approve.");
        Check(revision.StatusId == (int)QuotationStatus.Approved && revision.ApprovedByUserId == user.Id
            && revision.ApprovalDate.HasValue && revision.ApprovedDocumentJson!.Contains("Saved company"), "Approval must freeze document data and record its author and date.");
        var preview = new PreviewModel(db);
        Check(await preview.OnGetAsync(quote.Id, revision.Id) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
            && preview.Document.Revision.Total == 100, "Preview must use the saved revision amounts.");
        Check(await preview.OnGetAsync(quote.Id, Guid.NewGuid()) is NotFoundResult, "A foreign revision must not be previewed.");
        var again = Form(user.Id);
        await again.OnPostApproveAsync(quote.Id, revision.Id, version, true);
        Check(!again.ModelState.IsValid && await db.QuotationRevisions.CountAsync() == 1, "Repeated approval must not modify or duplicate the approved revision.");
        Console.WriteLine("PASS: GUID details/preview, generated references, confirmation, stale drafts, pending scopes, approval snapshot/audit and repeated approval.");
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
