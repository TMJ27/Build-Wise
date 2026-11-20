using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.ViewModels.Email;
using BuildWise.Web.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Email;

[Authorize]
public class DetailsModel(BuildWiseDbContext db, ICurrentUserService currentUser, IEmailSender sender) : PageModel
{
    public EmailConfiguration Settings { get; private set; } = null!;
    public List<EmailMessage> RecentMessages { get; private set; } = [];

    [BindProperty]
    public TestEmailInputViewModel Test { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync()) return RedirectToPage("./Upsert");
        return Page();
    }

    public async Task<IActionResult> OnPostTestAsync()
    {
        if (!await LoadAsync()) return RedirectToPage("./Upsert");
        if (!ModelState.IsValid) return Page();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Forbid();
        try
        {
            await sender.SendAsync(Test.RecipientEmail.Trim(), "BuildWise test email",
                "<p>This test email confirms that BuildWise can send through Amazon SES.</p>",
                userId.Value, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Amazon SES accepted the test email. Check the recipient's inbox and spam folder.";
            return RedirectToPage("./Details");
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync();
            return Page();
        }
    }

    private async Task<bool> LoadAsync()
    {
        var settings = await db.EmailConfigurations.AsNoTracking().Include(setting => setting.Creator)
            .OrderBy(setting => setting.CreationDate).ThenBy(setting => setting.Id).FirstOrDefaultAsync();
        if (settings is null) return false;
        Settings = settings;
        RecentMessages = await db.EmailMessages.AsNoTracking()
            .OrderByDescending(message => message.CreationDate).Take(10).ToListAsync();
        return true;
    }
}
