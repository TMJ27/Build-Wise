using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Customers.ViewModels;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Customers.Pages;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "requests";

    public Client Customer { get; private set; } = null!;
    public IReadOnlyList<ClientRequest> Requests { get; private set; } = [];
    public IReadOnlyList<CustomerQuotationHistoryViewModel> Quotations { get; private set; } = [];
    public IReadOnlyList<CustomerApprovedValueViewModel> ApprovedValues { get; private set; } = [];
    public int OpenCount => Requests.Count(request => request.StatusId == (int)ClientRequestStatus.Open);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var customer = await db.Clients.AsNoTracking().Include(client => client.Creator)
            .FirstOrDefaultAsync(client => client.Id == id);
        if (customer is null) return NotFound();
        Customer = customer;
        await db.ApplyClientNumberAsync(customer);
        Tab = Tab == "quotations" ? "quotations" : "requests";
        Requests = await db.ClientRequests.AsNoTracking()
            .Where(request => request.ClientId == id && request.StatusId != (int)ClientRequestStatus.Draft)
            .OrderByDescending(request => request.CreationDate).ThenBy(request => request.Id).ToListAsync();
        var quotations = await db.Quotations.AsNoTracking()
            .Where(quotation => quotation.Request.ClientId == id)
            .Include(quotation => quotation.QuotationRevisions).ToListAsync();
        await db.ApplyQuotationNumbersAsync(quotations);
        Quotations = quotations.Select(quotation => new
            {
                Quotation = quotation,
                Revision = quotation.QuotationRevisions.OrderByDescending(revision => revision.RevisionNumber).FirstOrDefault()
            })
            .Where(row => row.Revision is not null)
            .OrderByDescending(row => row.Revision!.CreationDate)
            .Select(row => new CustomerQuotationHistoryViewModel
            {
                Id = row.Quotation.Id, Number = row.Quotation.GeneratedQuotationNumber,
                Title = row.Revision!.Title, RevisionNumber = row.Revision.RevisionNumber,
                Status = (QuotationStatus)row.Revision.StatusId, IncludesPrices = row.Revision.IncludesPrices,
                CurrencyCode = row.Revision.CurrencyCode, Total = row.Revision.Total
            }).ToList();
        // Retain the previous approved value while a newer draft is prepared.
        ApprovedValues = quotations.Select(quotation => quotation.QuotationRevisions
                .Where(revision => revision.StatusId == (int)QuotationStatus.Approved)
                .OrderByDescending(revision => revision.RevisionNumber).FirstOrDefault())
            .Where(revision => revision is { IncludesPrices: true, Total: not null }
                && !string.IsNullOrWhiteSpace(revision.CurrencyCode))
            .GroupBy(revision => revision!.CurrencyCode!.ToUpperInvariant())
            .OrderBy(group => group.Key)
            .Select(group => new CustomerApprovedValueViewModel(group.Key, group.Sum(revision => revision!.Total!.Value)))
            .ToList();
        return Page();
    }
}
