using BuildWise.Data;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Dependencies;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    public Guid ScopeItemId { get; private set; }
    public Guid RequestId { get; private set; }
    public string ScopeItemTitle { get; private set; } = string.Empty;
    public bool CanEdit { get; private set; }
    public IReadOnlyList<ScopeDependencyDetailsViewModel> Dependencies { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid scopeItemId, Guid? requestId)
    {
        var item = await db.ScopeItems.AsNoTracking().Include(item => item.Request)
            .SingleOrDefaultAsync(item => item.Id == scopeItemId);
        if (item is null || (requestId.HasValue && item.RequestId != requestId)) return NotFound();
        ScopeItemId = item.Id;
        RequestId = item.RequestId;
        ScopeItemTitle = item.Title;
        CanEdit = ScopeQueries.CanEdit(item.Request.StatusId);
        var dependencies = await db.ScopeDependencies.AsNoTracking().Include(dependency => dependency.Creator)
            .Include(dependency => dependency.DependsOnScopeItem)
            .Include(dependency => dependency.ScopeItem).ThenInclude(parent => parent.Request)
            .Where(dependency => dependency.ScopeItemId == item.Id).OrderBy(dependency => dependency.CreationDate)
            .ThenBy(dependency => dependency.Id).ToListAsync();
        Dependencies = dependencies.Select(ScopeQueries.MapDependency).ToList();
        return Page();
    }
}
