using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting("CustomerLogin")]
public class LoginModel(
    BuildWiseDbContext db,
    IPasswordHasher<CustomerAccount> passwordHasher) : PageModel
{
    [BindProperty]
    public CustomerLoginViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var normalizedEmail = Input.Email.Trim().ToUpperInvariant();
        var account = await db.CustomerAccounts
            .Include(customerAccount => customerAccount.Client)
            .SingleOrDefaultAsync(customerAccount => customerAccount.NormalizedEmail == normalizedEmail);

        if (account is null
            || !account.IsActive
            || !account.Client.IsActive
            || string.IsNullOrWhiteSpace(account.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
            return Page();
        }

        var verification = passwordHasher.VerifyHashedPassword(
            account,
            account.PasswordHash,
            Input.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
            return Page();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = passwordHasher.HashPassword(account, Input.Password);
            await db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(CustomerPortalAuthentication.StampClaim, account.SecurityStamp),
            new(ClaimTypes.Name, account.Client.Name),
            new(ClaimTypes.Email, account.Email)
        };

        var identity = new ClaimsIdentity(claims, CustomerPortalAuthentication.Scheme);
        await HttpContext.SignInAsync(
            CustomerPortalAuthentication.Scheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return Redirect("/CustomerPortal");
    }
}
