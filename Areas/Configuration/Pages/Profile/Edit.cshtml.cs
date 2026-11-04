using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Areas.Configuration.ViewModels.Profile;

namespace BuildWise.Web.Areas.Configuration.Pages.Profile;

public class EditModel(BuildWiseDbContext context, IWebHostEnvironment environment) : PageModel
{
    private const int MaximumLogoBytes = 2 * 1024 * 1024;

    [BindProperty]
    public ProfileInputViewModel Input { get; set; } = new();

    public Company? Company { get; set; }
    public IReadOnlyList<BuildWise.Data.Currency> Currencies { get; private set; } = [];

    [BindProperty]
    public IFormFile? Logo { get; set; }

    [BindProperty]
    public bool RemoveLogo { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        Currencies = await context.Currencies.AsNoTracking().OrderBy(currency => currency.Name).ToListAsync();
        Company = id.HasValue
            ? await context.Companies.FirstOrDefaultAsync(x => x.Id == id.Value)
            : await context.Companies.SingleOrDefaultAsync();

        if(Company!= null)
        {

            Input.CompanyId = Company.Id;
            Input.Name = Company.Name;
            Input.RegistrationNumber = Company.RegistrationNumber;
            Input.TaxNumber = Company.TaxNumber;
            Input.Email = Company.Email;
            Input.Phone = Company.Phone;
            Input.Address = Company.Address;
            Input.DefaultCurrency = Company.DefaultCurrency ?? string.Empty;
            Input.DefaultTerms = Company.DefaultQuotationTerms ?? string.Empty;
            Input.DefaultValidityDays = Company.DefaultValidityDays;
        }
        else
        {
            return id.HasValue ? NotFound() : Page();
        }

        return Page(); 

    }

    public async Task<IActionResult> OnPostAsync()
    {
        Currencies = await context.Currencies.AsNoTracking().OrderBy(currency => currency.Name).ToListAsync();
        Company = await context.Companies.FirstOrDefaultAsync(x => x.Id == Input.CompanyId);

        if (!string.IsNullOrWhiteSpace(Input.DefaultCurrency) &&
            !Currencies.Any(currency => currency.Code == Input.DefaultCurrency))
        {
            ModelState.AddModelError("Input.DefaultCurrency", "Select a currency from the list.");
        }

        if (Input.CompanyId != Guid.Empty && Company is null)
        {
            return NotFound();
        }

        if (Input.CompanyId == Guid.Empty && await context.Companies.AnyAsync())
        {
            ModelState.AddModelError(string.Empty, "A company profile already exists. Open it to make changes.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }
        else
        {
            if (Company !=null)
            { 
                Company.Name = Input.Name;
                Company.RegistrationNumber = Input.RegistrationNumber ?? string.Empty;
                Company.TaxNumber = Input.TaxNumber ?? string.Empty;
                Company.Email = Input.Email;
                Company.Phone = Input.Phone;
                Company.Address = Input.Address;
                Company.DefaultValidityDays = Input.DefaultValidityDays;
                Company.DefaultQuotationTerms = Input.DefaultTerms;
                Company.DefaultCurrency = Input.DefaultCurrency;
                Company.LastUpdated = DateTime.UtcNow;

                if (RemoveLogo)
                {
                    Company.LogoStorageKey = null;
                }
                else if (Logo != null)
                {
                    if (Logo.Length > MaximumLogoBytes)
                    {
                        ModelState.AddModelError("Logo", "The logo file size exceeds the maximum allowed size of 2 MB.");
                        return Page();
                    }
                    var logoFileName = $"{Company.Id}{Path.GetExtension(Logo.FileName)}";
                    var logoPath = Path.Combine(environment.WebRootPath, "logos", logoFileName);
                    Directory.CreateDirectory(Path.Combine(environment.WebRootPath, "logos"));
                    using (var stream = new FileStream(logoPath, FileMode.Create))
                    {
                        await Logo.CopyToAsync(stream);
                    }
                    Company.LogoStorageKey = $"/logos/{logoFileName}";
                }

            }
            else
            {

                Company = new Company
                {
                    Id = Guid.NewGuid(),
                    Name = Input.Name,
                    RegistrationNumber = Input.RegistrationNumber ?? string.Empty,
                    TaxNumber = Input.TaxNumber ?? string.Empty,
                    Email = Input.Email,
                    Phone = Input.Phone,
                    Address = Input.Address,
                    DefaultValidityDays = Input.DefaultValidityDays,
                    DefaultQuotationTerms = Input.DefaultTerms,
                    DefaultCurrency = Input.DefaultCurrency,
                    CreationDate = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                };

                if (Logo != null)
                {
                    if (Logo.Length > MaximumLogoBytes)
                    {
                        ModelState.AddModelError("Logo", "The logo file size exceeds the maximum allowed size of 2 MB.");
                        return Page();
                    }
                    var logoFileName = $"{Company.Id}{Path.GetExtension(Logo.FileName)}";
                    var logoPath = Path.Combine(environment.WebRootPath, "logos", logoFileName);
                    Directory.CreateDirectory(Path.Combine(environment.WebRootPath, "logos"));
                    using (var stream = new FileStream(logoPath, FileMode.Create))
                    {
                        await Logo.CopyToAsync(stream);
                    }

                    
                    Company.LogoStorageKey = $"/logos/{logoFileName}";
                }

                await context.Companies.AddAsync(Company);
            }

            await context.SaveChangesAsync();

        }
       

        TempData["ProfileMessage"] = "Business profile saved. Your new quotation defaults are ready to use.";
        return RedirectToPage("/Profile/Details", new { id = Company!.Id });
    }

    
}
