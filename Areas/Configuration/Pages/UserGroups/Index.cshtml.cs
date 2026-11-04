using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.UserGroups;

public class IndexModel(BuildWiseDbContext context) : PageModel
{
    public IReadOnlyList<UserGroupRowViewModel> Groups { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Groups = await context.UserGroups.AsNoTracking()
            .OrderBy(group => group.Name)
            .Select(group => new UserGroupRowViewModel(group.Name, group.Users.Count, group.CreationDate))
            .ToListAsync();
    }
}
