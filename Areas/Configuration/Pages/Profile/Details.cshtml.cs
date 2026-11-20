using BuildWise.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Profile;

public class DetailsModel(BuildWiseDbContext context) : PageModel
{
    public Company? Company { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            Company = await context.Companies
                .FromSqlInterpolated($"SELECT * FROM dbo.Company WHERE Id = {id.Value}")
                .AsNoTracking()
                .SingleOrDefaultAsync();

            if (Company is null)
            {
                return NotFound();
            }
        }
        else
        {
            Company = await context.Companies
                .FromSqlRaw("SELECT * FROM dbo.Company")
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        return Page();
    }
}
