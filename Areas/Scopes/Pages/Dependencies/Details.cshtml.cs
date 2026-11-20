using BuildWise.Data;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Dependencies;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ScopeDependencyDetailsViewModel Dependency { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? scopeItemId)
    {
        var dependency = await db.ScopeDependencies.AsNoTracking().Include(dependency => dependency.Creator)
            .Include(dependency => dependency.DependsOnScopeItem)
            .Include(dependency => dependency.ScopeItem).ThenInclude(item => item.Request)
            .SingleOrDefaultAsync(dependency => dependency.Id == id);
        if (dependency is null || (scopeItemId.HasValue && dependency.ScopeItemId != scopeItemId)) return NotFound();
        Dependency = ScopeQueries.MapDependency(dependency);
        return Page();
    }
}
