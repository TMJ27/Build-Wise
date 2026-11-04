using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.Areas.Configuration.ViewModels.Access;

namespace BuildWise.Web.Areas.Configuration.Pages.Access;

public class IntegrationModel(AccessWorkspace access, CompanyService company) : PageModel
{
    [BindProperty]
    public IntegrationInputViewModel Input { get; set; } = new();

    public string Kind { get; private set; } = string.Empty;
    public bool IsAi => Kind == "ai";
    public string Title => IsAi ? "AI assistance" : "Email delivery";

    public async Task<IActionResult> OnGetAsync(string kind)
    {
        var preferences = access.FindIntegration(kind);

        if (preferences is null)
        {
            return NotFound();
        }

        Kind = kind;
        Input = new IntegrationInputViewModel
        {
            Provider = preferences.Provider,
            Model = preferences.Model,
            SenderName = preferences.SenderName,
            SenderEmail = preferences.SenderEmail,
            Host = preferences.Host,
            Port = preferences.Port,
            UseTls = preferences.UseTls,
            EnableWhenConnected = preferences.EnableWhenConnected,
            Version = preferences.Version
        };

        if (!IsAi && preferences.Version == 1)
        {
            var profile = await company.GetCurrentAsync(HttpContext.RequestAborted);
            Input.SenderName = profile?.Name ?? string.Empty;
            Input.SenderEmail = profile?.Email ?? string.Empty;
        }

        return Page();
    }

    public IActionResult OnPost(string kind)
    {
        if (access.FindIntegration(kind) is null)
        {
            return NotFound();
        }

        Kind = kind;

        if (IsAi)
        {
            if (Input.Provider is not ("OpenAI" or "Azure OpenAI"))
            {
                ModelState.AddModelError("Input.Provider", "Choose an AI provider.");
            }

            if (string.IsNullOrWhiteSpace(Input.Model))
            {
                ModelState.AddModelError("Input.Model", "Enter a model or deployment name.");
            }
        }
        else
        {
            if (Input.Provider is not ("SMTP" or "Microsoft 365"))
            {
                ModelState.AddModelError("Input.Provider", "Choose an email provider.");
            }

            if (string.IsNullOrWhiteSpace(Input.SenderName))
            {
                ModelState.AddModelError("Input.SenderName", "Enter the sender name.");
            }

            if (string.IsNullOrWhiteSpace(Input.SenderEmail))
            {
                ModelState.AddModelError("Input.SenderEmail", "Enter the sender email address.");
            }

            if (Input.Provider == "SMTP" && (string.IsNullOrWhiteSpace(Input.Host) ||
                Uri.CheckHostName(Input.Host.Trim()) == UriHostNameType.Unknown))
            {
                ModelState.AddModelError("Input.Host", "Enter a valid SMTP server hostname without a protocol or path.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var preferences = new IntegrationPreferences(
            kind,
            Input.Provider,
            IsAi ? (Input.Model ?? string.Empty).Trim() : string.Empty,
            IsAi ? string.Empty : (Input.SenderName ?? string.Empty).Trim(),
            IsAi ? string.Empty : (Input.SenderEmail ?? string.Empty).Trim(),
            !IsAi && Input.Provider == "SMTP" ? (Input.Host ?? string.Empty).Trim() : string.Empty,
            Input.Port,
            Input.UseTls,
            Input.EnableWhenConnected,
            Input.Version);

        if (!access.SaveIntegration(preferences, Input.Version))
        {
            ModelState.AddModelError(string.Empty, "These preferences changed in another window. Reload before saving again.");
            return Page();
        }

        TempData["AccessMessage"] = $"{Title} preferences saved. The connection has not been verified.";
        return RedirectToPage("/Access/Index");
    }
}
