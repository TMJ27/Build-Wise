using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Stages;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public ScopeStageUpsertViewModel Input { get; set; } = new();
    public Guid RequestId { get; private set; }
    public string ScopeItemTitle { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? scopeItemId)
    {
        if (id.HasValue)
        {
            var stage = await db.ScopeStages.AsNoTracking().SingleOrDefaultAsync(stage => stage.Id == id);
            if (stage is null || (scopeItemId.HasValue && stage.ScopeItemId != scopeItemId)) return NotFound();
            Input = new ScopeStageUpsertViewModel
            {
                Id = stage.Id, ScopeItemId = stage.ScopeItemId, Name = stage.Name, Description = stage.Description
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
        var stage = id.HasValue ? await db.ScopeStages.SingleOrDefaultAsync(stage => stage.Id == id) : null;
        if (id.HasValue && stage is null) return NotFound();
        if (stage is not null && stage.ScopeItemId != Input.ScopeItemId) return BadRequest();
        var item = await LoadParentAsync(Input.ScopeItemId);
        if (item is null) return NotFound();
        if (!ScopeQueries.CanEdit(item.Request.StatusId)) return StatusCode(403);

        Input.Name = (Input.Name ?? string.Empty).Trim();
        Input.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        if (Input.Name.Length is < 1 or > 50)
            ModelState.AddModelError("Input.Name", "Enter a stage name of 1 to 50 characters.");
        if (Input.Description?.Length > 4000)
            ModelState.AddModelError("Input.Description", "The description must not exceed 4000 characters.");
        if (!ModelState.IsValid) return Page();

        if (stage is null)
        {
            stage = new ScopeStage
            {
                Id = Guid.NewGuid(), ScopeItemId = item.Id, CreatorId = userId.Value, CreationDate = DateTime.UtcNow
            };
            db.ScopeStages.Add(stage);
        }
        var changed = stage.Name != Input.Name || stage.Description != Input.Description;
        stage.Name = Input.Name;
        stage.Description = Input.Description;
        if (changed)
        {
            item.ReviewStatusId = (int)ScopeReviewStatus.Pending;
            item.ReviewedOn = null;
        }
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = id.HasValue ? "Scope stage updated successfully." : "Scope stage created successfully.";
        return RedirectToPage("./Details", new { id = stage.Id, scopeItemId = stage.ScopeItemId });
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
