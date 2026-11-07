using BuildWise.Data;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Stages;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ScopeStageDetailsViewModel Stage { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? scopeItemId)
    {
        var stage = await db.ScopeStages.AsNoTracking().Include(stage => stage.Creator)
            .Include(stage => stage.ScopeItem).ThenInclude(item => item.Request)
            .SingleOrDefaultAsync(stage => stage.Id == id);
        if (stage is null || (scopeItemId.HasValue && stage.ScopeItemId != scopeItemId)) return NotFound();
        Stage = ScopeQueries.MapStage(stage);
        return Page();
    }
}
