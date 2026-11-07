using System.Data;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Dependencies;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public ScopeDependencyUpsertViewModel Input { get; set; } = new();
    public Guid RequestId { get; private set; }
    public string ScopeItemTitle { get; private set; } = string.Empty;
    public List<SelectListItem> Prerequisites { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? scopeItemId)
    {
        if (id.HasValue)
        {
            var dependency = await db.ScopeDependencies.AsNoTracking().SingleOrDefaultAsync(dependency => dependency.Id == id);
            if (dependency is null || (scopeItemId.HasValue && dependency.ScopeItemId != scopeItemId)) return NotFound();
            Input = new ScopeDependencyUpsertViewModel
            {
                Id = dependency.Id, ScopeItemId = dependency.ScopeItemId,
                DependsOnScopeItemId = dependency.DependsOnScopeItemId, Notes = ScopeQueries.ReadNotes(dependency.NotesJson)
            };
        }
        else
        {
            if (!scopeItemId.HasValue) return BadRequest();
            Input.ScopeItemId = scopeItemId.Value;
        }
        var item = await db.ScopeItems.AsNoTracking().Include(item => item.Request).SingleOrDefaultAsync(item => item.Id == Input.ScopeItemId);
        if (item is null) return NotFound();
        if (!ScopeQueries.CanEdit(item.Request.StatusId)) return StatusCode(403);
        await LoadContextAsync(item);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, Guid? scopeItemId)
    {
        if (id != Input.Id || (scopeItemId.HasValue && scopeItemId != Input.ScopeItemId)) return BadRequest();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();
        var requestId = await db.ScopeItems.AsNoTracking().Where(item => item.Id == Input.ScopeItemId)
            .Select(item => (Guid?)item.RequestId).SingleOrDefaultAsync();
        if (!requestId.HasValue) return NotFound();
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        // Serialize dependency edits for this request before reading its graph. This prevents two
        // concurrent saves from independently passing duplicate/cycle validation on SQL Server.
        var requestQuery = db.Database.IsSqlServer()
            ? db.ClientRequests.FromSqlInterpolated($"SELECT * FROM [ClientRequest] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {requestId.Value}")
            : db.ClientRequests.Where(request => request.Id == requestId.Value);
        var request = await requestQuery.SingleOrDefaultAsync();
        if (request is null) return NotFound();
        if (!ScopeQueries.CanEdit(request.StatusId)) return StatusCode(403);
        var dependency = id.HasValue ? await db.ScopeDependencies.SingleOrDefaultAsync(dependency => dependency.Id == id) : null;
        if (id.HasValue && dependency is null) return NotFound();
        if (dependency is not null && dependency.ScopeItemId != Input.ScopeItemId) return BadRequest();
        var item = await db.ScopeItems.SingleOrDefaultAsync(item => item.Id == Input.ScopeItemId);
        if (item is null) return NotFound();
        if (item.RequestId != request.Id) return BadRequest();
        Input.Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim();
        if (Input.Notes?.Length > 4000)
            ModelState.AddModelError("Input.Notes", "Notes must not exceed 4000 characters.");
        var target = await db.ScopeItems.AsNoTracking().SingleOrDefaultAsync(target => target.Id == Input.DependsOnScopeItemId);
        if (target is null || target.RequestId != item.RequestId)
            ModelState.AddModelError("Input.DependsOnScopeItemId", "Choose an existing scope item from this request.");
        else if (target.Id == item.Id)
            ModelState.AddModelError("Input.DependsOnScopeItemId", "An item cannot depend on itself.");
        else
        {
            var edges = await db.ScopeDependencies.AsNoTracking()
                .Where(edge => edge.ScopeItem.RequestId == item.RequestId && edge.Id != id)
                .Select(edge => new { edge.ScopeItemId, edge.DependsOnScopeItemId }).ToListAsync();
            if (edges.Any(edge => edge.ScopeItemId == item.Id && edge.DependsOnScopeItemId == target.Id))
                ModelState.AddModelError("Input.DependsOnScopeItemId", "This dependency already exists.");
            var graph = edges.ToLookup(edge => edge.ScopeItemId, edge => edge.DependsOnScopeItemId);
            var pending = new Stack<Guid>();
            var visited = new HashSet<Guid>();
            pending.Push(target.Id);
            while (pending.TryPop(out var next))
            {
                if (next == item.Id)
                {
                    ModelState.AddModelError("Input.DependsOnScopeItemId", "This would create a circular dependency.");
                    break;
                }
                if (!visited.Add(next)) continue;
                foreach (var prerequisite in graph[next]) pending.Push(prerequisite);
            }
        }
        if (!ModelState.IsValid)
        {
            await LoadContextAsync(item);
            return Page();
        }
        if (dependency is null)
        {
            dependency = new ScopeDependency
            {
                Id = Guid.NewGuid(), RequestId = item.RequestId, ScopeItemId = item.Id,
                CreatorId = userId.Value, CreationDate = DateTime.UtcNow
            };
            db.ScopeDependencies.Add(dependency);
        }
        var changed = dependency.DependsOnScopeItemId != target!.Id || ScopeQueries.ReadNotes(dependency.NotesJson) != Input.Notes;
        dependency.DependsOnScopeItemId = target.Id;
        dependency.RequestId = item.RequestId;
        if (ScopeQueries.ReadNotes(dependency.NotesJson) != Input.Notes)
            dependency.NotesJson = Input.Notes is null ? null : JsonSerializer.Serialize(new { notes = Input.Notes });
        if (changed)
        {
            item.ReviewStatusId = (int)ScopeReviewStatus.Pending;
            item.ReviewedOn = null;
        }
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = id.HasValue ? "Scope dependency updated successfully." : "Scope dependency created successfully.";
        return RedirectToPage("./Details", new { id = dependency.Id, scopeItemId = dependency.ScopeItemId });
    }

    private async Task LoadContextAsync(ScopeItem item)
    {
        RequestId = item.RequestId;
        ScopeItemTitle = item.Title;
        Prerequisites = await db.ScopeItems.AsNoTracking().Where(target => target.RequestId == item.RequestId && target.Id != item.Id)
            .OrderBy(target => target.Title).ThenBy(target => target.Id)
            .Select(target => new SelectListItem { Value = target.Id.ToString(), Text = target.Title }).ToListAsync();
    }
}
