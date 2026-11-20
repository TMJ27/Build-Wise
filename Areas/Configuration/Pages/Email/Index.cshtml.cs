using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.Email;

[Authorize]
public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Details");
}