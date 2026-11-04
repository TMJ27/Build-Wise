using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.Users;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Users;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    public IReadOnlyList<User> Users { get; set; } = new List<User>();

    public async Task OnGetAsync()
    {
      Users = await db.Users.ToListAsync();
    }

}

