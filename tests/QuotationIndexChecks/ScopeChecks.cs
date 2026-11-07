using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using ScopeIndex = BuildWise.Web.Areas.Scopes.Pages.IndexModel;
using ScopeDetails = BuildWise.Web.Areas.Scopes.Pages.DetailsModel;
using ItemDetails = BuildWise.Web.Areas.Scopes.Pages.Items.DetailsModel;
using ItemForm = BuildWise.Web.Areas.Scopes.Pages.Items.UpsertModel;

internal static class ScopeChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "scope@example.test", PasswordHash = "test" };
        var client = new Client { Id = Guid.NewGuid(), Name = "Customer", Email = "customer@example.test", Address = "Harare", Creator = user };
        var request = new ClientRequest
        {
            Id = Guid.NewGuid(), Title = "Paint house", SiteAddress = "Harare", ReferenceNumber = "REQ-01",
            Client = client, Creator = user, StatusId = (int)ClientRequestStatus.Open
        };
        var other = new ClientRequest
        {
            Id = Guid.NewGuid(), Title = "Other request", SiteAddress = "Harare", Client = client,
            Creator = user, StatusId = (int)ClientRequestStatus.Open
        };
        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Painting", IsActive = true };
        var service = new Service
        {
            Id = Guid.NewGuid(), Name = "Painting", IsActive = true, RequireSiteAssessment = true,
            Category = category, Creator = user, CreationDate = new DateTime(2026, 1, 1)
        };
        var inactive = new Service { Id = Guid.NewGuid(), Name = "Inactive", IsActive = false, Category = category, Creator = user, CreationDate = new DateTime(2026, 1, 2) };
        var unit = new Unit { Id = Guid.NewGuid(), Code = "rooms", Name = "Rooms", IsActive = true, Creator = user };
        db.AddRange(user, client, request, other, category, service, inactive, unit,
            new ServiceUnit { Id = Guid.NewGuid(), Service = service, Unit = unit, IsDefault = true });
        await db.SaveChangesAsync();
        ItemForm Form(Guid? userId) => new(db, new CurrentUser(userId))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        var index = new ScopeIndex(db);
        await index.OnGetAsync();
        Check(index.Requests.Count == 2 && index.Requests.All(r => r.ItemCount == 0), "Requests without items must appear in Scope.");
        var form = Form(user.Id);
        Check(await form.OnGetAsync(null, request.Id) is PageResult && form.Services.Count == 1,
            "Create form must load request context and active services only.");
        form.Input = new ScopeItemUpsertViewModel
        {
            RequestId = request.Id, ServiceId = service.Id, Title = " Paint walls ", Description = " Two coats ",
            Quantity = 3, AssessmentNotes = " Check access ", MarkReviewed = true
        };
        Check(await form.OnPostAsync(null, request.Id) is RedirectToPageResult, "A valid scope item must save.");
        var item = await db.ScopeItems.SingleAsync();
        Check(item.Title == "Paint walls" && item.Description == "Two coats" && item.UnitCodeSnapshot == "rooms"
            && item.ServiceCodeSnapshot == "SRV-0001", "Saved fields, default units and catalogue snapshots must be correct.");
        Check(item.RequireAssessment && ScopeQueries.ReadNotes(item.AssessmentNotesJson) == "Check access"
            && item.ReviewStatusId == (int)ScopeReviewStatus.Reviewed && item.ReviewedByUserId == user.Id
            && item.ReviewedOn.HasValue, "Assessment and authenticated review metadata must be saved.");
        var details = new ScopeDetails(db);
        Check(await details.OnGetAsync(request.Id) is PageResult && details.Items.Count == 1
            && details.Scope.AssessmentCount == 1, "Request scope must show only its own items and real counts.");
        await details.OnGetAsync(other.Id);
        Check(details.Items.Count == 0, "An item must not leak into another request.");
        var itemDetails = new ItemDetails(db);
        Check(await itemDetails.OnGetAsync(item.Id, other.Id) is NotFoundResult, "A mismatched parent must be rejected.");
        Check(await itemDetails.OnGetAsync(item.Id, request.Id) is PageResult && itemDetails.Item.ReviewedBy == user.Email,
            "Item details must load service and reviewer with email fallback.");

        var stage = new ScopeStage { Id = Guid.NewGuid(), ScopeItem = item, Name = "Prepare walls", Creator = user };
        db.ScopeStages.Add(stage);
        await db.SaveChangesAsync();
        var edit = Form(user.Id);
        await edit.OnGetAsync(item.Id, request.Id);
        edit.Input.Title = "Updated scope";
        edit.Input.Quantity = 5;
        edit.Input.MarkReviewed = false;
        var code = item.ServiceCodeSnapshot;
        var json = item.AssessmentNotesJson;
        Check(await edit.OnPostAsync(item.Id, request.Id) is RedirectToPageResult && await db.ScopeItems.CountAsync() == 1,
            "Editing must update rather than duplicate.");
        Check(item.Title == "Updated scope" && item.ReviewStatusId == (int)ScopeReviewStatus.Pending
            && item.ReviewedOn == null && item.ServiceCodeSnapshot == code && item.AssessmentNotesJson == json
            && await db.ScopeStages.CountAsync() == 1, "Edits must preserve snapshots, notes and related records while updating review state.");

        var tamper = Form(user.Id);
        await tamper.OnGetAsync(item.Id, request.Id);
        tamper.Input.RequestId = other.Id;
        Check(await tamper.OnPostAsync(item.Id, null) is BadRequestResult, "An existing item cannot move to another request.");
        var invalid = Form(user.Id);
        invalid.Input = new() { RequestId = request.Id, ServiceId = inactive.Id, Title = "Inactive service" };
        Check(await invalid.OnPostAsync(null, null) is PageResult && !invalid.ModelState.IsValid, "Inactive services must be rejected for new items.");
        var missingService = Form(user.Id);
        missingService.Input = new() { RequestId = request.Id, ServiceId = Guid.NewGuid(), Title = "Missing service" };
        Check(await missingService.OnPostAsync(null, null) is PageResult, "Missing services must be rejected without saving.");
        var whitespace = Form(user.Id);
        whitespace.Input = new() { RequestId = request.Id, ServiceId = service.Id, Title = " ", Quantity = -1 };
        Check(await whitespace.OnPostAsync(null, null) is PageResult && !whitespace.ModelState.IsValid, "Invalid titles and quantities must be rejected.");
        service.IsActive = false;
        await db.SaveChangesAsync();
        var inactiveEdit = Form(user.Id);
        await inactiveEdit.OnGetAsync(item.Id, request.Id);
        Check(inactiveEdit.Services.Any(s => s.Value == service.Id.ToString()), "Existing inactive service must remain editable.");
        Check(await inactiveEdit.OnPostAsync(item.Id, request.Id) is RedirectToPageResult, "Editing an existing inactive service association must work.");
        request.StatusId = (int)ClientRequestStatus.Closed;
        await db.SaveChangesAsync();
        Check(await Form(user.Id).OnGetAsync(item.Id, request.Id) is StatusCodeResult { StatusCode: 403 }, "Closed requests must be read-only.");
        Check(await inactiveEdit.OnPostAsync(item.Id, request.Id) is PageResult && !inactiveEdit.ModelState.IsValid,
            "A request closed after loading the form must reject saving.");
        Check(await Form(null).OnPostAsync(null, null) is UnauthorizedResult, "Saving requires an authenticated user.");
        Check(await new ScopeDetails(db).OnGetAsync(Guid.NewGuid()) is NotFoundResult
            && await new ItemDetails(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult, "Missing records must return 404.");
        Check(ScopeQueries.ReadNotes("legacy text") == "legacy text"
            && ScopeQueries.ReadNotes("\"string notes\"") == "string notes", "Legacy notes must remain readable.");
        var quotation = new BuildWise.Web.Areas.Quotations.Pages.UpsertModel(db, new CurrentUser(user.Id));
        await quotation.OnGetAsync(null, other.Id);
        Check(quotation.Input.Items.Count == 0, "Quotation loading must not import scope from a different request.");
        request.StatusId = (int)ClientRequestStatus.Open;
        await db.SaveChangesAsync();
        Check(await quotation.OnGetAsync(null, request.Id) is PageResult
            && quotation.Input.Items.Single().ScopeItemId == item.Id && quotation.Input.Items.Single().Title == item.Title,
            "The saved scope must load into quotation creation for the same request.");
        await ScopeClauseChecks.RunAsync(db, user, item, other);
        await ScopeStageChecks.RunAsync(db, user, item, other);
        await ScopeDependencyChecks.RunAsync(db, user, item, other);
        Console.WriteLine("PASS: request scopes, item creation/editing, catalogue snapshots, assessments, review, validation, inactive services, closed requests and identity checks.");
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
