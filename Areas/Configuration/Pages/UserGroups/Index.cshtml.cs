using BuildWise.Data;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.UserGroups;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
  
    public IReadOnlyList<UserGroup> UserGroups { get; set; } = new List<UserGroup>();


    public async Task OnGetAsync()
    {
        UserGroups = await db.UserGroups.ToListAsync();
        await db.ApplyUserGroupNumbersAsync(UserGroups);
    }
}
