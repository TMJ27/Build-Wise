using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Data;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ScopeViewModel Scope { get; set; } = new();
    public IReadOnlyList<ScopeItemDetailsViewModel> Items { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var request = await ScopeQueries.Requests(db).SingleOrDefaultAsync(request => request.RequestId == id);
        if (request is null) return NotFound();
        Scope = request;
        var items = await db.ScopeItems.AsNoTracking().Include(item => item.Service)
            .Include(item => item.Request).Include(item => item.ReviewedByUser)
            .Where(item => item.RequestId == id).OrderBy(item => item.Title).ThenBy(item => item.Id).ToListAsync();
        Items = items.Select(ScopeQueries.MapItem).ToList();
        return Page();
    }
}
