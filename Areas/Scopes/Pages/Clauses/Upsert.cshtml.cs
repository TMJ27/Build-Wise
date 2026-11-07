using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Clauses;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public ScopeClauseUpsertViewModel Input { get; set; } = new();

    public Guid RequestId { get; private set; }
    public string ScopeItemTitle { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? scopeItemId)
    {
        if (id.HasValue)
        {
            var clause = await db.ScopeClauses.AsNoTracking().SingleOrDefaultAsync(clause => clause.Id == id);
            if (clause is null || (scopeItemId.HasValue && clause.ScopeItemId != scopeItemId)) return NotFound();
            Input = new ScopeClauseUpsertViewModel
            {
                Id = clause.Id, ScopeItemId = clause.ScopeItemId,
                ClauseType = Enum.IsDefined(typeof(ScopeClauseType), clause.ClauseTypeId)
                    ? (ScopeClauseType)clause.ClauseTypeId : null,
                Description = clause.Description ?? string.Empty
            };
        }
        else
        {
            if (!scopeItemId.HasValue) return BadRequest();
            Input.ScopeItemId = scopeItemId.Value;
        }
        var item = await LoadParentAsync(Input.ScopeItemId);
        if (item is null) return NotFound();
        if (!ScopeQueries.CanEdit(item.Request.StatusId)) return StatusCode(403);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, Guid? scopeItemId)
    {
        if (id != Input.Id || (scopeItemId.HasValue && scopeItemId != Input.ScopeItemId)) return BadRequest();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();
        var clause = id.HasValue ? await db.ScopeClauses.SingleOrDefaultAsync(clause => clause.Id == id) : null;
        if (id.HasValue && clause is null) return NotFound();
        if (clause is not null && clause.ScopeItemId != Input.ScopeItemId) return BadRequest();
        var item = await LoadParentAsync(Input.ScopeItemId);
        if (item is null) return NotFound();
        if (!ScopeQueries.CanEdit(item.Request.StatusId)) return StatusCode(403);

        Input.Description = (Input.Description ?? string.Empty).Trim();
        if (Input.Description.Length is < 1 or > 4000)
            ModelState.AddModelError("Input.Description", "Enter clause wording of 1 to 4000 characters.");
        if (!Input.ClauseType.HasValue || !Enum.IsDefined(Input.ClauseType.Value))
            ModelState.AddModelError("Input.ClauseType", "Choose an inclusion, exclusion or condition.");
        if (!ModelState.IsValid) return Page();

        if (clause is null)
        {
            clause = new ScopeClause
            {
                Id = Guid.NewGuid(), ScopeItemId = item.Id, CreatorId = userId.Value, CreationDate = DateTime.UtcNow
            };
            db.ScopeClauses.Add(clause);
        }
        var changed = clause.ClauseTypeId != (int)Input.ClauseType!.Value || clause.Description != Input.Description;
        clause.ClauseTypeId = (int)Input.ClauseType.Value;
        clause.Description = Input.Description;
        if (changed)
        {
            // Review applies to the whole work item, including its clauses.
            item.ReviewStatusId = (int)ScopeReviewStatus.Pending;
            item.ReviewedOn = null;
        }
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = id.HasValue ? "Scope clause updated successfully." : "Scope clause created successfully.";
        return RedirectToPage("./Details", new { id = clause.Id, scopeItemId = clause.ScopeItemId });
    }

    private async Task<ScopeItem?> LoadParentAsync(Guid id)
    {
        var item = await db.ScopeItems.Include(item => item.Request).SingleOrDefaultAsync(item => item.Id == id);
        if (item is not null)
        {
            RequestId = item.RequestId;
            ScopeItemTitle = item.Title;
        }
        return item;
    }
}
