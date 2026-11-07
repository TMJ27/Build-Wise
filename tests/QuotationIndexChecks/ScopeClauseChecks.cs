using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.Pages.Clauses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class ScopeClauseChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync(BuildWiseDbContext db, User user, ScopeItem item, ClientRequest otherRequest)
    {
        UpsertModel Form(Guid? userId) => new(db, new CurrentUser(userId))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        foreach (var type in Enum.GetValues<ScopeClauseType>())
        {
            var create = Form(user.Id);
            Check(await create.OnGetAsync(null, item.Id) is PageResult && create.RequestId == item.RequestId,
                "Clause creation must load its scope item and request context.");
            create.Input.ClauseType = type;
            create.Input.Description = "  " + type + " wording  ";
            Check(await create.OnPostAsync(null, item.Id) is RedirectToPageResult, "All clause types must save.");
        }
        var clauses = await db.ScopeClauses.Where(c => c.ScopeItemId == item.Id).ToListAsync();
        Check(clauses.Count == 3 && clauses.All(c => c.CreatorId == user.Id && c.CreationDate.HasValue
            && c.Description == ((ScopeClauseType)c.ClauseTypeId) + " wording"),
            "Clause saves must trim wording and track the actual creator and UTC creation date.");
        Check(item.ReviewStatusId == (int)ScopeReviewStatus.Pending && item.ReviewedOn == null,
            "Clause changes must invalidate scope review.");
        var inclusion = clauses.Single(c => c.ClauseTypeId == (int)ScopeClauseType.Inclusion);
        var details = new DetailsModel(db);
        Check(await details.OnGetAsync(inclusion.Id, item.Id) is PageResult
            && details.Clause.CreatorName == user.Email && details.Clause.ClauseTypeName == "Inclusion",
            "Clause details must load type, parent and creator.");
        Check(await details.OnGetAsync(inclusion.Id, Guid.NewGuid()) is NotFoundResult,
            "Clause details must reject another parent context.");
        var itemDetails = new BuildWise.Web.Areas.Scopes.Pages.Items.DetailsModel(db);
        await itemDetails.OnGetAsync(item.Id, item.RequestId);
        Check(itemDetails.Clauses.Count == 3, "Item details must list its clauses.");
        var index = new IndexModel(db);
        Check(await index.OnGetAsync(item.Id, item.RequestId) is PageResult && index.Clauses.Count == 3
            && index.ScopeItemId == item.Id && index.RequestId == item.RequestId && index.CanEdit,
            "Clause index must load the correct item and its clauses.");
        Check(await new IndexModel(db).OnGetAsync(item.Id, otherRequest.Id) is NotFoundResult
            && await new IndexModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult,
            "Clause index must reject missing items and mismatched requests.");

        var creatorId = inclusion.CreatorId;
        var creationDate = inclusion.CreationDate;
        item.ReviewStatusId = (int)ScopeReviewStatus.Reviewed;
        item.ReviewedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var unchanged = Form(user.Id);
        await unchanged.OnGetAsync(inclusion.Id, null);
        Check(await unchanged.OnPostAsync(inclusion.Id, item.Id) is RedirectToPageResult && item.ReviewedOn.HasValue,
            "Saving unchanged wording must preserve review state.");
        var edit = Form(user.Id);
        await edit.OnGetAsync(inclusion.Id, item.Id);
        edit.Input.Description = "Updated wording";
        edit.Input.ClauseType = ScopeClauseType.Condition;
        Check(await edit.OnPostAsync(inclusion.Id, item.Id) is RedirectToPageResult && inclusion.Description == "Updated wording"
            && inclusion.ClauseTypeId == (int)ScopeClauseType.Condition && inclusion.CreatorId == creatorId
            && inclusion.CreationDate == creationDate && item.ReviewedOn == null,
            "Edits must update the existing clause, preserve creation metadata and reset review.");

        var otherItem = new ScopeItem
        {
            Id = Guid.NewGuid(), RequestId = otherRequest.Id, ServiceId = item.ServiceId, Title = "Other work",
            ServiceCodeSnapshot = "OTHER", ReviewedByUserId = user.Id
        };
        db.ScopeItems.Add(otherItem);
        await db.SaveChangesAsync();
        var otherDetails = new BuildWise.Web.Areas.Scopes.Pages.Items.DetailsModel(db);
        await otherDetails.OnGetAsync(otherItem.Id, otherRequest.Id);
        Check(otherDetails.Clauses.Count == 0, "Clauses must not appear under another scope item.");
        var emptyIndex = new IndexModel(db);
        Check(await emptyIndex.OnGetAsync(otherItem.Id, null) is PageResult && emptyIndex.Clauses.Count == 0,
            "Clause index must show an empty list for a valid item without clauses.");
        var moved = Form(user.Id);
        await moved.OnGetAsync(inclusion.Id, item.Id);
        moved.Input.ScopeItemId = otherItem.Id;
        Check(await moved.OnPostAsync(inclusion.Id, null) is BadRequestResult, "Existing clauses cannot move between items.");
        var invalid = Form(user.Id);
        invalid.Input.ScopeItemId = item.Id;
        invalid.Input.ClauseType = (ScopeClauseType)999;
        invalid.Input.Description = "Invalid type";
        Check(await invalid.OnPostAsync(null, item.Id) is PageResult && !invalid.ModelState.IsValid,
            "Unknown clause types must be rejected.");
        var blank = Form(user.Id);
        blank.Input.ScopeItemId = item.Id;
        blank.Input.Description = "   ";
        Check(await blank.OnPostAsync(null, item.Id) is PageResult && !blank.ModelState.IsValid,
            "Whitespace wording must be rejected.");
        Check(await Form(user.Id).OnGetAsync(null, Guid.NewGuid()) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult,
            "Missing parent or clause must return 404.");
        Check(await Form(null).OnPostAsync(null, null) is UnauthorizedResult, "Clause saves require an authenticated user.");
        item.Request.StatusId = (int)ClientRequestStatus.cancelled;
        await db.SaveChangesAsync();
        Check(await Form(user.Id).OnGetAsync(inclusion.Id, item.Id) is StatusCodeResult { StatusCode: 403 },
            "Cancelled requests must reject editing clauses.");
        Check(await edit.OnPostAsync(inclusion.Id, item.Id) is StatusCodeResult { StatusCode: 403 },
            "A request cancelled after loading must reject the save.");
        var readonlyDetails = new DetailsModel(db);
        Check(await readonlyDetails.OnGetAsync(inclusion.Id, item.Id) is PageResult && !readonlyDetails.Clause.CanEdit,
            "Closed/cancelled request clauses must remain viewable.");
        var readonlyIndex = new IndexModel(db);
        Check(await readonlyIndex.OnGetAsync(item.Id, null) is PageResult && !readonlyIndex.CanEdit
            && readonlyIndex.Clauses.Count == 3, "Clause index must preserve read-only access for cancelled requests.");
        item.Request.StatusId = (int)ClientRequestStatus.Open;
        await db.SaveChangesAsync();
        Console.WriteLine("PASS: clause types, create/edit/details, item isolation, validation, creation metadata, review invalidation and read-only request guards.");
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
