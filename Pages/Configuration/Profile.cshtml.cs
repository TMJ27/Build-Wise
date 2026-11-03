using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Configuration;

public class ProfileModel(CompanyWorkspace company) : PageModel
{
    private const int MaximumLogoBytes = 2 * 1024 * 1024;

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    [BindProperty]
    public IFormFile? Logo { get; set; }

    [BindProperty]
    public bool RemoveLogo { get; set; }

    public CompanyProfile Profile { get; private set; } = null!;

    public void OnGet()
    {
        Profile = company.Current;
        Input = new ProfileInput
        {
            Name = Profile.Name,
            Email = Profile.Email,
            Phone = Profile.Phone,
            Address = Profile.Address,
            RegistrationNumber = Profile.RegistrationNumber,
            TaxNumber = Profile.TaxNumber,
            DefaultValidityDays = Profile.DefaultValidityDays,
            DefaultTerms = Profile.DefaultTerms,
            DefaultExclusions = Profile.DefaultExclusions,
            Version = Profile.Version
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Profile = company.Current;
        var logoDataUrl = Profile.LogoDataUrl;

        if (RemoveLogo && Logo is not null)
        {
            ModelState.AddModelError(nameof(Logo), "Choose a new logo or remove the current logo, rather than both.");
        }
        else if (RemoveLogo)
        {
            logoDataUrl = null;
        }
        else if (Logo is not null)
        {
            if (Logo.Length is < 1 or > MaximumLogoBytes)
            {
                ModelState.AddModelError(nameof(Logo), "Choose a PNG or JPEG image up to 2 MB.");
            }
            else
            {
                await using var stream = new MemoryStream();
                await Logo.CopyToAsync(stream);
                var bytes = stream.ToArray();
                var isPng = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
                var isJpeg = bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });

                if (!isPng && !isJpeg)
                {
                    ModelState.AddModelError(nameof(Logo), "The logo must be a PNG or JPEG image.");
                }
                else
                {
                    var mediaType = isPng ? "image/png" : "image/jpeg";
                    logoDataUrl = $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}";
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var updated = Profile with
        {
            Name = Input.Name.Trim(),
            Email = Input.Email.Trim(),
            Phone = Input.Phone.Trim(),
            Address = Input.Address.Trim(),
            RegistrationNumber = (Input.RegistrationNumber ?? string.Empty).Trim(),
            TaxNumber = (Input.TaxNumber ?? string.Empty).Trim(),
            LogoDataUrl = logoDataUrl,
            DefaultValidityDays = Input.DefaultValidityDays,
            DefaultTerms = Input.DefaultTerms.Trim(),
            DefaultExclusions = Input.DefaultExclusions.Trim()
        };

        if (!company.Save(updated, Input.Version))
        {
            ModelState.AddModelError(string.Empty, "The business profile changed in another window. Reload this page before saving again.");
            return Page();
        }

        TempData["ProfileMessage"] = "Business profile saved. Your new quotation defaults are ready to use.";
        return RedirectToPage();
    }

    public sealed class ProfileInput
    {
        [Required, StringLength(30)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(50)]
        public string Email { get; set; } = string.Empty;

        [Required, Phone, StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RegistrationNumber { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [Range(1, 365)]
        public int DefaultValidityDays { get; set; } = 14;

        [Required, StringLength(2000)]
        public string DefaultTerms { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string DefaultExclusions { get; set; } = string.Empty;

        [Range(1, long.MaxValue)]
        public long Version { get; set; }
    }
}
