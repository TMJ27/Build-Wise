using BuildWise.Data;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Web.Areas.Configuration.ViewModels.Currency;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Currencies;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public CurrencyDetailsViewModel CurrencyDetails { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var currency = await db.Currencies.AsNoTracking()
            .Include(currency => currency.Creator)
            .FirstOrDefaultAsync(currency => currency.Id == id);

        if (currency is null)
        {
            return NotFound();
        }

        await db.ApplyCurrencyNumberAsync(currency);
        var creatorName = $"{currency.Creator.Name} {currency.Creator.Surname}".Trim();
        CurrencyDetails = new CurrencyDetailsViewModel
        {
            Id = currency.Id,
            CurrencyNumber = currency.CurrencyNumber,
            Name = currency.Name,
            Code = currency.Code,
            Symbol = currency.Symbol,
            Creator = string.IsNullOrWhiteSpace(creatorName) ? currency.Creator.Email : creatorName,
            CreationDate = currency.CreationDate
        };

        return Page();
    }
}
