using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    [BindProperty]
    public CustomerLoginViewModel Input { get; set; } = new();

    // UI only. Implement authentication and redirect to the customer dashboard before enabling submit.
}
