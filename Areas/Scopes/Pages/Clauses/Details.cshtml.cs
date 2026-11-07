using BuildWise.Data;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Clauses;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ScopeClauseDetailsViewModel Clause { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? scopeItemId)
    {
        var clause = await db.ScopeClauses.AsNoTracking().Include(clause => clause.Creator)
            .Include(clause => clause.ScopeItem).ThenInclude(item => item.Request)
            .SingleOrDefaultAsync(clause => clause.Id == id);
        if (clause is null || (scopeItemId.HasValue && clause.ScopeItemId != scopeItemId)) return NotFound();
        Clause = ScopeQueries.MapClause(clause);
        return Page();
    }
}
