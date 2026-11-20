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
public class ForgotPasswordModel(CustomerPasswordRecoveryService recovery) : PageModel
{
    [BindProperty] public ForgotPasswordInputViewModel Input { get; set; } = new();
    public bool Submitted { get; private set; }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Page();
        await recovery.RequestAsync(Input.Email, cancellationToken);
        Submitted = true;
        return Page();
    }
}
