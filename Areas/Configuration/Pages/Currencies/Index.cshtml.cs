using BuildWise.Data;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Web.Areas.Configuration.ViewModels.Currency;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Currencies;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    public IReadOnlyList<CurrencyIndexViewModel> Currencies { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var currencies = await db.Currencies.AsNoTracking()
            .Include(currency => currency.Creator)
            .OrderBy(currency => currency.Name)
            .ToListAsync();

        await db.ApplyCurrencyNumbersAsync(currencies);

        Currencies = currencies.Select(currency => new CurrencyIndexViewModel(
                currency.Id,
                currency.Name,
                currency.Code,
                currency.Symbol,
                currency.Creator.Name == null || currency.Creator.Name == ""
                    ? currency.Creator.Email
                    : ((currency.Creator.Name ?? "") + " " + (currency.Creator.Surname ?? "")).Trim(),
                currency.CreationDate)
            {
                CurrencyNumber = currency.CurrencyNumber
            })
            .ToArray();
    }
}
