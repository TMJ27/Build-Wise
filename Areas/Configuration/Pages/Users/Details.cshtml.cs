using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using BuildWise.Web.Areas.Configuration.ViewModels.Users;

namespace BuildWise.Web.Areas.Configuration.Pages.Users
{
 public class DetailsModel(BuildWiseDbContext db ) : PageModel
    {
      public User? Person { get; set; } = new();

      public UserUpsertViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGet(Guid? id)
        {
            Person = db.Users.FirstOrDefault(x => x.Id == id);
            if(Person == null)
            {
                return NotFound();
            }

            return Page();

        }

    }
}
