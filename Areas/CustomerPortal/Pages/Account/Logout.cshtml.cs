using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

[AllowAnonymous]
public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CustomerPortalAuthentication.Scheme);
        return RedirectToPage("./Login");
    }
}
