using BuildWise.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BuildWise.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LogoutModel(CognitoSettings settings) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!settings.IsConfigured)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        var logoutUrl = QueryHelpers.AddQueryString(settings.Domain.TrimEnd('/') + "/logout",
            new Dictionary<string, string?>
            {
                ["client_id"] = settings.ClientId,
                ["logout_uri"] = settings.LogoutUri
            });

        return Redirect(logoutUrl);
    }
}
