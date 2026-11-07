using BuildWise.Data;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Quotations;

public class DetailsModel(CustomerPortalService portal) : PageModel
{
    public QuotationRevision Revision { get; private set; } = null!;
    public string QuotationNumber { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        var revision = await portal.Quotations(account).Include(revision => revision.Quotation)
            .Include(revision => revision.QuotationItems).SingleOrDefaultAsync(revision => revision.QuotationId == id);
        if (revision is null) return NotFound();
        Revision = revision;
        QuotationNumber = revision.Quotation.QuotationNumber;
        return Page();
    }
}
