using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Lib.Enums.Statuses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages;

public class IndexModel(CustomerPortalService portal) : PageModel
{
    public CustomerDashboardViewModel Dashboard { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        var requests = portal.Requests(account);
        var quotations = portal.Quotations(account);
        Dashboard = new CustomerDashboardViewModel
        {
            CustomerName = account.Client.Name,
            RequestCount = await requests.CountAsync(),
            OpenRequestCount = await requests.CountAsync(request => request.StatusId != (int)ClientRequestStatus.Closed
                && request.StatusId != (int)ClientRequestStatus.cancelled),
            ApprovedQuotationCount = await quotations.CountAsync(),
            Requests = await requests.OrderByDescending(request => request.CreationDate).ThenBy(request => request.Id).Take(10)
                .Select(request => new CustomerRequestRowViewModel
                {
                    Id = request.Id, ReferenceNumber = request.ReferenceNumber, Title = request.Title,
                    StatusId = request.StatusId, CreationDate = request.CreationDate
                }).ToListAsync(),
            Quotations = await quotations.OrderByDescending(revision => revision.ApprovalDate).ThenBy(revision => revision.Id).Take(10)
                .Select(revision => new CustomerQuotationRowViewModel
                {
                    Id = revision.QuotationId, QuotationNumber = revision.Quotation.QuotationNumber,
                    Title = revision.Title, RevisionNumber = revision.RevisionNumber, Status = "Approved",
                    IncludesPrices = revision.IncludesPrices, Total = revision.Total, CurrencyCode = revision.CurrencyCode
                }).ToListAsync()
        };
        return Page();
    }
}
