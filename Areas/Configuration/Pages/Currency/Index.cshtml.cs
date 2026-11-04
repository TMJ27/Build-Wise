using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.Currency;
using BuildWise.Web.Areas.Configuration.ViewModels.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.Currency;

public class IndexModel(BuildWiseDbContext context) : PageModel
{
    [BindProperty]
    public CurrencyInputViewModel Input { get; set; } = new();

    public IReadOnlyList<CurrencyRowViewModel> Currencies { get; private set; } = [];
    public IReadOnlyList<UserOptionViewModel> Creators { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var currency = await context.Currencies.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id.Value);

            if (currency is null)
            {
                return NotFound();
            }

            Input = new CurrencyInputViewModel
            {
                Id = currency.Id,
                Name = currency.Name,
                Code = currency.Code,
                Symbol = currency.Symbol,
                CreatorId = currency.CreatorId
            };
        }

        await LoadDataAsync();

        if (!id.HasValue && Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            && Creators.Any(user => user.Id == userId))
        {
            Input.CreatorId = userId;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadDataAsync();
        var code = (Input.Code ?? string.Empty).Trim().ToUpperInvariant();
        var existing = Input.Id.HasValue
            ? await context.Currencies.AsNoTracking().SingleOrDefaultAsync(row => row.Id == Input.Id.Value)
            : null;

        if (Input.Id.HasValue && existing is null)
        {
            return NotFound();
        }

        if (existing is not null && (existing.Code != code || existing.CreatorId != Input.CreatorId))
        {
            ModelState.AddModelError(string.Empty, "The code and creator cannot be changed after a currency is created.");
        }

        if (!Creators.Any(user => user.Id == Input.CreatorId))
        {
            ModelState.AddModelError("Input.CreatorId", "Select an existing user.");
        }

        if (Currencies.Any(currency => currency.Code == code && currency.Id != Input.Id))
        {
            ModelState.AddModelError("Input.Code", "This currency code already exists.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            if (existing is null)
            {
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO dbo.Currency (Id, Name, Code, Symbol, CreatorId, CreationDate)
                    VALUES ({Guid.NewGuid()}, {Input.Name.Trim()}, {code}, {Input.Symbol.Trim()},
                            {Input.CreatorId!.Value}, {DateTime.UtcNow});
                    """);
            }
            else
            {
                var rows = await context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE dbo.Currency
                    SET Name = {Input.Name.Trim()}, Symbol = {Input.Symbol.Trim()}
                    WHERE Id = {existing.Id};
                    """);

                if (rows != 1)
                {
                    return NotFound();
                }
            }
        }
        catch (SqlException error) when (error.Number is 2601 or 2627)
        {
            ModelState.AddModelError("Input.Code", "This currency code already exists.");
            return Page();
        }

        TempData["CurrencyMessage"] = existing is null ? "Currency added." : "Currency updated.";
        return RedirectToPage("/Currency/Index");
    }

    private async Task LoadDataAsync()
    {
        Currencies = await context.Currencies.AsNoTracking()
            .OrderBy(currency => currency.Name)
            .Select(currency => new CurrencyRowViewModel(
                currency.Id, currency.Name, currency.Code, currency.Symbol,
                ((currency.Creator.Name ?? "") + " " + (currency.Creator.Surname ?? "")).Trim(),
                currency.CreationDate))
            .ToListAsync();

        Creators = await context.Users.AsNoTracking()
            .Where(user => user.IsActive || user.Id == Input.CreatorId)
            .OrderBy(user => user.Name)
            .Select(user => new UserOptionViewModel(user.Id,
                ((user.Name ?? "") + " " + (user.Surname ?? "")).Trim() + " · " + user.Email))
            .ToListAsync();
    }
}
