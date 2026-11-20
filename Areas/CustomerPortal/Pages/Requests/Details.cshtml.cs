using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Requests;

public class DetailsModel(BuildWiseDbContext db, CustomerPortalService portal) : PageModel
{
    public ClientRequest RequestDetails { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        var request = await portal.Requests(account).SingleOrDefaultAsync(request => request.Id == id);
        if (request is null) return NotFound();
        RequestDetails = request;
        return Page();
    }

    public async Task<IActionResult> OnPostSubmitAsync(Guid id)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        var request = await db.ClientRequests.SingleOrDefaultAsync(request => request.Id == id
            && request.ClientId == account.ClientId && request.StatusId == (int)ClientRequestStatus.Draft);
        if (request is null) return NotFound();
        await using var transaction = string.IsNullOrWhiteSpace(request.ReferenceNumber)
            ? await RequestReferenceGenerator.BeginCreationTransactionAsync(db)
            : null;
        if (string.IsNullOrWhiteSpace(request.ReferenceNumber))
            request.ReferenceNumber = await RequestReferenceGenerator.GenerateAsync(db, request.CreationDate);
        request.StatusId = (int)ClientRequestStatus.Open;
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Your request has been submitted.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        var request = await db.ClientRequests.SingleOrDefaultAsync(request => request.Id == id
            && request.ClientId == account.ClientId && request.StatusId == (int)ClientRequestStatus.Draft);
        if (request is null) return NotFound();
        request.StatusId = (int)ClientRequestStatus.cancelled;
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Your request draft has been cancelled.";
        return RedirectToPage(new { id });
    }
}
