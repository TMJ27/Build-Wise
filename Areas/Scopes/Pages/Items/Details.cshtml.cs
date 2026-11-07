using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Data;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Items;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ScopeItemDetailsViewModel Item { get; set; } = new();
    public IReadOnlyList<ScopeClauseDetailsViewModel> Clauses { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? requestId)
    {
        var item = await db.ScopeItems.AsNoTracking().Include(item => item.Service)
            .Include(item => item.Request).Include(item => item.ReviewedByUser).SingleOrDefaultAsync(item => item.Id == id);
        if (item is null || (requestId.HasValue && item.RequestId != requestId)) return NotFound();
        Item = ScopeQueries.MapItem(item);
        var clauses = await db.ScopeClauses.AsNoTracking().Include(clause => clause.Creator)
            .Include(clause => clause.ScopeItem).ThenInclude(parent => parent.Request)
            .Where(clause => clause.ScopeItemId == item.Id).OrderBy(clause => clause.ClauseTypeId)
            .ThenBy(clause => clause.CreationDate).ThenBy(clause => clause.Id).ToListAsync();
        Clauses = clauses.Select(ScopeQueries.MapClause).ToList();
        return Page();
    }
}
