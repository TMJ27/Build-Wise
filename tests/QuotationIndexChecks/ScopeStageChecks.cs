using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.Pages.Stages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class ScopeStageChecks
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
        var count = await db.ScopeStages.CountAsync(s => s.ScopeItemId == item.Id);
        var form = Form(user.Id);
        Check(await form.OnGetAsync(null, item.Id) is PageResult && form.RequestId == item.RequestId, "Create must load parent context.");
        form.Input.Name = " Apply primer ";
        form.Input.Description = " One coat ";
        Check(await form.OnPostAsync(null, item.Id) is RedirectToPageResult, "A valid stage must save.");
        var stage = await db.ScopeStages.SingleAsync(s => s.Name == "Apply primer");
        Check(stage.Description == "One coat" && stage.CreatorId == user.Id && stage.CreationDate.Kind == DateTimeKind.Utc
            && stage.ScopeItemId == item.Id, "Saving must trim fields and record the parent and authenticated creator.");
        Check(item.ReviewStatusId == (int)ScopeReviewStatus.Pending && item.ReviewedOn == null, "Stage creation must invalidate review.");
        var index = new IndexModel(db);
        Check(await index.OnGetAsync(item.Id, item.RequestId) is PageResult && index.Stages.Count == count + 1
            && index.Stages.Select(s => s.CreationDate).SequenceEqual(index.Stages.Select(s => s.CreationDate).Order()), "Index must list stages in creation order.");
        var details = new DetailsModel(db);
        Check(await details.OnGetAsync(stage.Id, item.Id) is PageResult && details.Stage.CreatorName == user.Email, "Details must load creator and parent.");
        Check(await new IndexModel(db).OnGetAsync(item.Id, otherRequest.Id) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(stage.Id, Guid.NewGuid()) is NotFoundResult, "Mismatched parent context must be rejected.");
        var otherItem = await db.ScopeItems.FirstAsync(s => s.RequestId == otherRequest.Id);
        var empty = new IndexModel(db);
        Check(await empty.OnGetAsync(otherItem.Id, null) is PageResult && empty.Stages.Count == 0, "Stages must not leak into other items.");

        item.ReviewStatusId = (int)ScopeReviewStatus.Reviewed;
        item.ReviewedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var edit = Form(user.Id);
        await edit.OnGetAsync(stage.Id, item.Id);
        Check(await edit.OnPostAsync(stage.Id, item.Id) is RedirectToPageResult && item.ReviewedOn.HasValue, "Unchanged saves must preserve review.");
        var date = stage.CreationDate;
        edit.Input.Name = "Prime walls";
        edit.Input.Description = " ";
        Check(await edit.OnPostAsync(stage.Id, item.Id) is RedirectToPageResult && stage.Name == "Prime walls"
            && stage.Description == null && stage.CreatorId == user.Id && stage.CreationDate == date && item.ReviewedOn == null
            && await db.ScopeStages.CountAsync(s => s.ScopeItemId == item.Id) == count + 1, "Edits must update in place, preserve metadata and reset review.");
        var moved = Form(user.Id);
        await moved.OnGetAsync(stage.Id, item.Id);
        moved.Input.ScopeItemId = otherItem.Id;
        Check(await moved.OnPostAsync(stage.Id, null) is BadRequestResult, "Stages cannot move between items.");
        foreach (var name in new[] { " ", new string('x', 51) })
        {
            var invalid = Form(user.Id);
            invalid.Input.ScopeItemId = item.Id;
            invalid.Input.Name = name;
            Check(await invalid.OnPostAsync(null, item.Id) is PageResult && !invalid.ModelState.IsValid, "Invalid names must be rejected.");
        }
        var longDescription = Form(user.Id);
        longDescription.Input.ScopeItemId = item.Id;
        longDescription.Input.Name = "Valid";
        longDescription.Input.Description = new string('x', 4001);
        Check(await longDescription.OnPostAsync(null, item.Id) is PageResult && !longDescription.ModelState.IsValid, "Oversized descriptions must be rejected.");
        Check(await new IndexModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult
            && await new DetailsModel(db).OnGetAsync(Guid.NewGuid(), null) is NotFoundResult
            && await Form(user.Id).OnGetAsync(null, Guid.NewGuid()) is NotFoundResult, "Missing records must return 404.");
        Check(await Form(null).OnPostAsync(null, null) is UnauthorizedResult, "Saving requires authentication.");
        item.Request.StatusId = (int)ClientRequestStatus.Closed;
        await db.SaveChangesAsync();
        Check(await Form(user.Id).OnGetAsync(stage.Id, item.Id) is StatusCodeResult { StatusCode: 403 }
            && await edit.OnPostAsync(stage.Id, item.Id) is StatusCodeResult { StatusCode: 403 }, "Closed requests must reject editing and saving.");
        var readonlyIndex = new IndexModel(db);
        Check(await readonlyIndex.OnGetAsync(item.Id, null) is PageResult && !readonlyIndex.CanEdit, "Closed request stages must remain viewable.");
        item.Request.StatusId = (int)ClientRequestStatus.Open;
        await db.SaveChangesAsync();
        Console.WriteLine("PASS: stage index/details, create/edit, validation, parent isolation, metadata, review invalidation and closed request guards.");
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
