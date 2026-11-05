using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.ViewModels.Currency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Currencies;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public CurrencyInputViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return Page();
        }

        var currency = await db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(currency => currency.Id == id.Value);

        if (currency is null)
        {
            return NotFound();
        }

        Input.Id = currency.Id;
        Input.Name = currency.Name;
        Input.Code = currency.Code;
        Input.Symbol = currency.Symbol;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (id != Input.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Input.Name = Input.Name.Trim();
        Input.Code = Input.Code.Trim().ToUpperInvariant();
        Input.Symbol = Input.Symbol.Trim();

        if (await db.Currencies.AnyAsync(currency => currency.Code == Input.Code && currency.Id != Input.Id))
        {
            ModelState.AddModelError("Input.Code", "A currency with this code already exists.");
            return Page();
        }

        var isNew = !id.HasValue;

        Currency currency;

        if (id.HasValue)
        {
            var existingCurrency = await db.Currencies.FirstOrDefaultAsync(currency => currency.Id == id.Value);

            if (existingCurrency is null)
            {
                return NotFound();
            }

            currency = existingCurrency;
        }
        else
        {
            var creatorId = await currentUser.GetUserIdAsync();

            if (!creatorId.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Sign in with an active BuildWise user account before creating a currency.");
                return Page();
            }

            currency = new Currency
            {
                Id = Guid.NewGuid(),
                CreatorId = creatorId.Value,
                CreationDate = DateTime.UtcNow
            };

            db.Currencies.Add(currency);
        }

        currency.Name = Input.Name;
        currency.Code = Input.Code;
        currency.Symbol = Input.Symbol;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            ModelState.AddModelError("Input.Code", "A currency with this code already exists.");
            return Page();
        }

        TempData["SuccessMessage"] = isNew
            ? "Currency created successfully."
            : "Currency updated successfully.";

        return RedirectToPage("./Details", new { id = currency.Id });
    }
}
