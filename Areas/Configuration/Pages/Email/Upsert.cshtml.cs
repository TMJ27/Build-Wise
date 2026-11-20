using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.ViewModels.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Email;

[Authorize]
public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public EmailSettingsInputViewModel Input { get; set; } = new();

    public bool IsConfigured { get; private set; }

    public async Task OnGetAsync()
    {
        var settings = await db.EmailConfigurations.AsNoTracking()
            .OrderBy(setting => setting.CreationDate).ThenBy(setting => setting.Id).FirstOrDefaultAsync();
        IsConfigured = settings is not null;
        if (settings is not null)
            Input = new EmailSettingsInputViewModel
            {
                SenderName = settings.SenderName, SenderEmail = settings.SenderEmail,
                ReplyToEmail = settings.ReplyToEmail, Region = settings.Region, IsEnabled = settings.IsEnabled
            };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var settings = await db.EmailConfigurations.OrderBy(setting => setting.CreationDate)
            .ThenBy(setting => setting.Id).FirstOrDefaultAsync();
        IsConfigured = settings is not null;
        if (!ModelState.IsValid) return Page();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Forbid();
        if (settings is null)
        {
            settings = new EmailConfiguration
            {
                Id = Guid.NewGuid(), CreatorId = userId.Value, CreationDate = DateTime.Now
            };
            db.EmailConfigurations.Add(settings);
        }
        else
        {
            settings.LastUpdate = DateTime.Now;
        }
        settings.SenderName = Input.SenderName.Trim();
        settings.SenderEmail = Input.SenderEmail.Trim();
        settings.ReplyToEmail = string.IsNullOrWhiteSpace(Input.ReplyToEmail) ? null : Input.ReplyToEmail.Trim();
        settings.Region = Input.Region.Trim();
        settings.IsEnabled = Input.IsEnabled;
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Email settings saved.";
        return RedirectToPage("./Details");
    }
}
