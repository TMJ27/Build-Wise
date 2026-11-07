using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Data;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    public IReadOnlyList<ScopeViewModel> Requests { get; set; } = [];

    public async Task OnGetAsync()
    {
        // Requests without items are included so their scope can be prepared.
        Requests = await ScopeQueries.Requests(db).OrderBy(request => request.Title)
            .ThenBy(request => request.RequestId).ToListAsync();
    }
}
