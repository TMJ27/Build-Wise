using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.CustomerPortal.Pages;

[AllowAnonymous]
public class WelcomeModel : PageModel
{
    public void OnGet() { }
}
