using BuildWise.Data;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Lib.Enums.Statuses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages;

public class IndexModel(BuildWiseDbContext db, CustomerPortalService portal) : PageModel
{
    public CustomerDashboardViewModel Dashboard { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        var requests = portal.Requests(account);
        var quotations = portal.Quotations(account);
        var requestRows = await requests.OrderByDescending(request => request.CreationDate).ThenBy(request => request.Id).Take(10).ToListAsync();
        var quotationRows = await quotations.OrderByDescending(revision => revision.ApprovalDate).ThenBy(revision => revision.Id).Take(10)
            .Select(revision => new
            {
                Revision = revision,
                QuotationId = revision.QuotationId,
                ResponseStatus = db.QuotationResponses
                    .Where(response => response.QuotationRevisionId == revision.Id)
                    .Select(response => (QuotationResponseStatus?)response.StatusId).FirstOrDefault()
            }).ToListAsync();
        var quotationIds = quotationRows.Select(row => row.QuotationId).Distinct().ToHashSet();
        var numberedQuotations = await db.Quotations.AsNoTracking().Where(quotation => quotationIds.Contains(quotation.Id)).ToListAsync();
        await db.ApplyQuotationNumbersAsync(numberedQuotations);
        var quotationNumbers = numberedQuotations.ToDictionary(quotation => quotation.Id, quotation => quotation.GeneratedQuotationNumber);
        Dashboard = new CustomerDashboardViewModel
        {
            CustomerName = account.Client.Name,
            RequestCount = await requests.CountAsync(),
            OpenRequestCount = await requests.CountAsync(request => request.StatusId != (int)ClientRequestStatus.Closed
                && request.StatusId != (int)ClientRequestStatus.cancelled
                && request.StatusId != (int)ClientRequestStatus.Draft),
            ClosedRequestCount = await requests.CountAsync(request => request.StatusId == (int)ClientRequestStatus.Closed),
            ApprovedQuotationCount = await quotations.CountAsync(),
            Requests = requestRows.Select(request => new CustomerRequestRowViewModel
                {
                    Id = request.Id, ReferenceNumber = request.ReferenceNumber, Title = request.Title,
                    StatusId = request.StatusId, CreationDate = request.CreationDate
                }).ToList(),
            Quotations = quotationRows.Select(row => new CustomerQuotationRowViewModel
                {
                    Id = row.Revision.QuotationId, QuotationNumber = quotationNumbers.GetValueOrDefault(row.QuotationId, ""),
                    Title = row.Revision.Title, RevisionNumber = row.Revision.RevisionNumber, Status = (QuotationStatus)row.Revision.StatusId,
                    IncludesPrices = row.Revision.IncludesPrices, Total = row.Revision.Total, CurrencyCode = row.Revision.CurrencyCode,
                    ResponseStatus = row.ResponseStatus
                }).ToList()
        };
        return Page();
    }
}
