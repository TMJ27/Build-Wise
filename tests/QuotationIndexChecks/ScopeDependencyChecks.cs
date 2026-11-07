using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.Pages.Dependencies;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class ScopeDependencyChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static async Task RunAsync(BuildWiseDbContext db, User user, ScopeItem item, ClientRequest otherRequest)
    {
        ScopeItem Work(string title) => new()
        {
            Id = Guid.NewGuid(), RequestId = item.RequestId, ServiceId = item.ServiceId,
            Title = title, ServiceCodeSnapshot = item.ServiceCodeSnapshot, ReviewedByUserId = user.Id
        };
        var second = Work("Second work");
        var third = Work("Third work");
        var fourth = Work("Fourth work");
        db.ScopeItems.AddRange(second, third, fourth);
        await db.SaveChangesAsync();
        var foreign = await db.ScopeItems.FirstAsync(i => i.RequestId == otherRequest.Id);
        UpsertModel Form(Guid source, Guid? target, Guid? userId) => new(db, new CurrentUser(userId))
        {
            Input = new() { ScopeItemId = source, DependsOnScopeItemId = target },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        var create = Form(item.Id, second.Id, user.Id);
        Check(await create.OnGetAsync(null, item.Id) is PageResult && create.Prerequisites.Count == 3
            && create.Prerequisites.All(p => p.Value != item.Id.ToString() && p.Value != foreign.Id.ToString()),
            "Options must include only other items in the same request.");
        create.Input.Notes = " Finish this first ";
        Check(await create.OnPostAsync(null, item.Id) is RedirectToPageResult, "Valid dependency must save.");
        var dependency = await db.ScopeDependencies.SingleAsync(d => d.ScopeItemId == item.Id);
        Check(dependency.DependsOnScopeItemId == second.Id && dependency.RequestId == item.RequestId
            && dependency.CreatorId == user.Id && dependency.CreationDate.Kind == DateTimeKind.Utc
            && ScopeQueries.ReadNotes(dependency.NotesJson) == "Finish this first", "Saving must derive request, creator and JSON notes correctly.");
        Check(item.ReviewStatusId == (int)ScopeReviewStatus.Pending && item.ReviewedOn == null, "Changes must reset item review.");
        var duplicate = Form(item.Id, second.Id, user.Id);
        Check(await duplicate.OnPostAsync(null, item.Id) is PageResult && !duplicate.ModelState.IsValid, "Duplicate dependency must be rejected.");
        foreach (var target in new Guid?[] { null, item.Id, foreign.Id, Guid.NewGuid() })
        {
            var invalid = Form(item.Id, target, user.Id);
            Check(await invalid.OnPostAsync(null, item.Id) is PageResult && !invalid.ModelState.IsValid,
                "Missing, self, cross-request and unknown targets must be rejected.");
        }
        var reverse = Form(second.Id, item.Id, user.Id);
        Check(await reverse.OnPostAsync(null, second.Id) is PageResult && !reverse.ModelState.IsValid, "Direct circular dependency must be rejected.");
        Check(await Form(second.Id, third.Id, user.Id).OnPostAsync(null, second.Id) is RedirectToPageResult,
            "Non-circular chains must save.");
        var longCycle = Form(third.Id, item.Id, user.Id);
        Check(await longCycle.OnPostAsync(null, third.Id) is PageResult && !longCycle.ModelState.IsValid,
            "Indirect circular dependencies must be rejected.");
        var details = new DetailsModel(db);
        Check(await details.OnGetAsync(dependency.Id, item.Id) is PageResult && details.Dependency.DependsOnTitle == second.Title
            && details.Dependency.CreatorName == user.Email, "Details must show target and creator.");
        var index = new IndexModel(db);
        Check(await index.OnGetAsync(item.Id, item.RequestId) is PageResult && index.Dependencies.Count == 1,
            "Index must list only this item's prerequisites.");
        var empty = new IndexModel(db);
        Check(await empty.OnGetAsync(fourth.Id, null) is PageResult && empty.Dependencies.Count == 0,
            "Items without dependencies must load an empty list.");
        Check(await new IndexModel(db).OnGetAsync(item.Id, otherRequest.Id) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(dependency.Id, fourth.Id) is NotFoundResult,
            "Mismatched route parents must be rejected.");
        item.ReviewStatusId = (int)ScopeReviewStatus.Reviewed;
        item.ReviewedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var edit = Form(item.Id, second.Id, user.Id);
        await edit.OnGetAsync(dependency.Id, item.Id);
        Check(await edit.OnPostAsync(dependency.Id, item.Id) is RedirectToPageResult && item.ReviewedOn.HasValue,
            "Saving unchanged dependency must not flag a duplicate or reset review.");
        var date = dependency.CreationDate;
        edit.Input.DependsOnScopeItemId = fourth.Id;
        edit.Input.Notes = " Updated note ";
        Check(await edit.OnPostAsync(dependency.Id, item.Id) is RedirectToPageResult
            && dependency.DependsOnScopeItemId == fourth.Id && dependency.CreationDate == date
            && dependency.CreatorId == user.Id && item.ReviewedOn == null, "Edits must preserve creation metadata and reset review.");
        var moved = Form(item.Id, fourth.Id, user.Id);
        await moved.OnGetAsync(dependency.Id, item.Id);
        moved.Input.ScopeItemId = third.Id;
        Check(await moved.OnPostAsync(dependency.Id, null) is BadRequestResult, "Dependencies cannot move between source items.");
        Check(await Form(item.Id, second.Id, null).OnPostAsync(null, item.Id) is UnauthorizedResult, "Saving requires authentication.");
        Check(await new IndexModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult,
            "Missing records must return 404.");
        var oversized = Form(third.Id, fourth.Id, user.Id);
        oversized.Input.Notes = new string('x', 4001);
        Check(await oversized.OnPostAsync(null, third.Id) is PageResult && !oversized.ModelState.IsValid,
            "Oversized notes must be rejected.");
        item.Request.StatusId = (int)ClientRequestStatus.Closed;
        await db.SaveChangesAsync();
        Check(await edit.OnPostAsync(dependency.Id, item.Id) is StatusCodeResult { StatusCode: 403 }
            && await Form(item.Id, second.Id, user.Id).OnGetAsync(null, item.Id) is StatusCodeResult { StatusCode: 403 },
            "Closed requests must reject saving and editing.");
        var readOnly = new IndexModel(db);
        Check(await readOnly.OnGetAsync(item.Id, null) is PageResult && !readOnly.CanEdit, "Closed requests must remain viewable.");
        item.Request.StatusId = (int)ClientRequestStatus.Open;
        await db.SaveChangesAsync();
        Console.WriteLine("PASS: dependency create/edit/index/details, duplicate/self/cross-request/cycle checks, audit metadata and request guards.");
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
