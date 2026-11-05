using BuildWise.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(CognitoSettings settings, IWebHostEnvironment environment) : PageModel
{
    [BindProperty]
    public string ReturnUrl { get; set; } = "/";

    public bool IsConfigured => settings.IsConfigured;

    public string? SignInDiagnostic { get; private set; }

    public IActionResult OnGet(string? returnUrl, string? error)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        if (error == "signin")
        {
            ModelState.AddModelError(string.Empty, "Sign-in could not be completed. Please try again.");
            if (environment.IsDevelopment())
            {
                SignInDiagnostic = TempData["SignInDiagnostic"] as string;
            }
        }

        return Page();
    }

    public IActionResult OnPost()
    {
        var returnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl);
        }

        if (!settings.IsConfigured)
        {
            ModelState.AddModelError(string.Empty, "Sign-in is unavailable. Please contact your administrator.");
            return Page();
        }

        return Challenge(new AuthenticationProperties { RedirectUri = returnUrl },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
