using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting("CustomerPasswordRecovery")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ResetPasswordModel(CustomerPasswordRecoveryService recovery) : PageModel
{
    [BindProperty] public ResetPasswordInputViewModel Input { get; set; } = new();
    public bool LinkValid { get; private set; }

    public async Task OnGetAsync(string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        LinkValid = await recovery.IsValidAsync(token);
        if (LinkValid) Input.Token = token!;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        LinkValid = await recovery.IsValidAsync(Input.Token);
        if (!LinkValid) return Page();
        if (!ModelState.IsValid) return Page();
        if (!await recovery.ResetAsync(Input.Token, Input.Password))
        {
            LinkValid = false;
            return Page();
        }
        TempData["SuccessMessage"] = "Your password has been reset. Sign in with your new password.";
        return RedirectToPage("Login");
    }
}
