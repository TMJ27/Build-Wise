using BuildWise.Data;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Requests;

public class DetailsModel(CustomerPortalService portal) : PageModel
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
}
