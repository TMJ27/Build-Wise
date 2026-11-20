using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildWise.Lib.Enums;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

public class SettingsModel(
    CustomerPortalService portal,
    CustomerAccountSettingsService accountSettings) : PageModel
{
    [BindProperty]
    public CustomerProfileInputViewModel Profile { get; set; } = new();

    [BindProperty]
    public CustomerPasswordInputViewModel Password { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        FillProfile(account);
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;

        Profile.Name = (Profile.Name ?? string.Empty).Trim();
        Profile.Email = (Profile.Email ?? string.Empty).Trim();
        Profile.Phone = Clean(Profile.Phone);
        Profile.Address = (Profile.Address ?? string.Empty).Trim();
        if (!Enum.IsDefined(Profile.ClientType)) ModelState.AddModelError("Profile.ClientType", "Choose a customer type.");
        if (Profile.Name.Length is < 1 or > 50) ModelState.AddModelError("Profile.Name", "Enter a name of 1 to 50 characters.");
        if (Profile.Email.Length is < 1 or > 50 || !new EmailAddressAttribute().IsValid(Profile.Email))
            ModelState.AddModelError("Profile.Email", "Enter a valid email address of no more than 50 characters.");
        if (Profile.Phone?.Length > 20 || (Profile.Phone is not null && !new PhoneAttribute().IsValid(Profile.Phone)))
            ModelState.AddModelError("Profile.Phone", "Enter a valid phone number of no more than 20 characters.");
        if (Profile.Address.Length is < 1 or > 50) ModelState.AddModelError("Profile.Address", "Enter an address of 1 to 50 characters.");
        if (!ModelState.IsValid) return Page();

        var result = await accountSettings.UpdateProfileAsync(account.Id, Profile.Name, Profile.Email,
            Profile.Phone, Profile.Address, (int)Profile.ClientType);
        if (result.EmailInUse)
        {
            ModelState.AddModelError("Profile.Email", "That email address is already connected to an account.");
            return Page();
        }
        if (result.Account is null) return Challenge(CustomerPortalAuthentication.Scheme);

        await RefreshCustomerSignInAsync(result.Account.Id, result.Account.SecurityStamp,
            result.Account.Client.Name, result.Account.Email);
        TempData["AccountSuccessMessage"] = "Your account details have been updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPasswordAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        FillProfile(account);

        var currentPassword = Password.CurrentPassword ?? string.Empty;
        var newPassword = Password.NewPassword ?? string.Empty;
        var confirmPassword = Password.ConfirmPassword ?? string.Empty;
        if (string.IsNullOrEmpty(currentPassword))
            ModelState.AddModelError("Password.CurrentPassword", "Enter your current password.");
        if (newPassword.Length is < 12 or > 128)
            ModelState.AddModelError("Password.NewPassword", "Use a new password of 12 to 128 characters.");
        if (newPassword != confirmPassword)
            ModelState.AddModelError("Password.ConfirmPassword", "The passwords must match.");
        if (newPassword == currentPassword && !string.IsNullOrEmpty(newPassword))
            ModelState.AddModelError("Password.NewPassword", "Choose a password different from your current one.");
        if (!ModelState.IsValid) return Page();

        var updated = await accountSettings.ChangePasswordAsync(account.Id, currentPassword, newPassword);
        if (updated is null)
        {
            ModelState.AddModelError("Password.CurrentPassword", "Your current password is incorrect.");
            return Page();
        }

        await RefreshCustomerSignInAsync(updated.Id, updated.SecurityStamp, updated.Client.Name, updated.Email);
        TempData["AccountSuccessMessage"] = "Your password has been changed.";
        return RedirectToPage();
    }

    private void FillProfile(BuildWise.Data.CustomerAccount account)
    {
        ViewData["CustomerName"] = account.Client.Name;
        Profile = new CustomerProfileInputViewModel
        {
            ClientType = Enum.IsDefined(typeof(ClientType), account.Client.ClientTypeId)
                ? (ClientType)account.Client.ClientTypeId : ClientType.Individual,
            Name = account.Client.Name,
            Email = account.Email,
            Phone = account.Client.Phone,
            Address = account.Client.Address
        };
    }

    private async Task RefreshCustomerSignInAsync(Guid accountId, string securityStamp, string name, string email)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString()),
            new(CustomerPortalAuthentication.StampClaim, securityStamp),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, CustomerPortalAuthentication.Scheme);
        await HttpContext.SignInAsync(CustomerPortalAuthentication.Scheme, new ClaimsPrincipal(identity));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
