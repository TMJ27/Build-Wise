using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Account;

[AllowAnonymous]
[EnableRateLimiting("CustomerRegistration")]
public class RegisterModel(CustomerRegistrationService registration) : PageModel
{
    [BindProperty]
    public CustomerRegistrationViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Name = (Input.Name ?? string.Empty).Trim();
        Input.Email = (Input.Email ?? string.Empty).Trim();
        Input.Address = (Input.Address ?? string.Empty).Trim();
        if (Input.Name.Length is < 1 or > 50) ModelState.AddModelError("Input.Name", "Enter a name of 1 to 50 characters.");
        if (Input.Address.Length is < 1 or > 50) ModelState.AddModelError("Input.Address", "Enter an address of 1 to 50 characters.");
        if (!Enum.IsDefined(Input.ClientType)) ModelState.AddModelError("Input.ClientType", "Choose a customer type.");
        if (Input.Password is null || Input.Password.Length is < 12 or > 128)
            ModelState.AddModelError("Input.Password", "Use a password of 12 to 128 characters.");
        if (Input.Password != Input.ConfirmPassword) ModelState.AddModelError("Input.ConfirmPassword", "The passwords must match.");
        if (!ModelState.IsValid) return Page();
        if (!await registration.RegisterAsync(Input))
        {
            ModelState.AddModelError(string.Empty, "An account with these details cannot be created. Try signing in or contact the business.");
            return Page();
        }
        TempData["SuccessMessage"] = "Your account has been created. Sign in to access your dashboard.";
        return RedirectToPage("./Login");
    }
}
