using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Requests;

public class CreateModel(BuildWiseDbContext db, CustomerPortalService portal) : PageModel
{
    [BindProperty] public CustomerRequestInputViewModel Input { get; set; } = new();
    public Guid? DraftId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid? id = null)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        Input.SiteAddress = account.Client.Address;
        if (id.HasValue)
        {
            var request = await portal.Requests(account).SingleOrDefaultAsync(request => request.Id == id.Value
                && request.StatusId == (int)ClientRequestStatus.Draft);
            if (request is null) return NotFound();
            DraftId = request.Id;
            Input.Title = request.Title;
            Input.SiteAddress = request.SiteAddress;
            Input.PropertyType = request.PropertyType;
            Input.Description = request.Description ?? string.Empty;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id = null)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        DraftId = id;
        Input.Title = (Input.Title ?? string.Empty).Trim();
        Input.SiteAddress = (Input.SiteAddress ?? string.Empty).Trim();
        Input.Description = (Input.Description ?? string.Empty).Trim();
        Input.PropertyType = string.IsNullOrWhiteSpace(Input.PropertyType) ? null : Input.PropertyType.Trim();
        if (Input.Title.Length is < 1 or > 50) ModelState.AddModelError("Input.Title", "Enter a title of 1 to 50 characters.");
        if (Input.SiteAddress.Length is < 1 or > 50) ModelState.AddModelError("Input.SiteAddress", "Enter a site address of 1 to 50 characters.");
        if (Input.Description.Length is < 1 or > 3000) ModelState.AddModelError("Input.Description", "Enter a description of 1 to 3000 characters.");
        if (Input.PropertyType?.Length > 50) ModelState.AddModelError("Input.PropertyType", "Property type must not exceed 50 characters.");
        if (!ModelState.IsValid) return Page();
        var request = id.HasValue
            ? await db.ClientRequests.SingleOrDefaultAsync(request => request.Id == id.Value
                && request.ClientId == account.ClientId && request.StatusId == (int)ClientRequestStatus.Draft)
            : null;
        if (id.HasValue && request is null) return NotFound();
        var now = DateTime.Now;
        var needsReference = request is null || string.IsNullOrWhiteSpace(request.ReferenceNumber);
        await using var transaction = needsReference
            ? await RequestReferenceGenerator.BeginCreationTransactionAsync(db)
            : null;
        if (request is null)
        {
            request = new ClientRequest
            {
                Id = Guid.NewGuid(), ClientId = account.ClientId, CreatorId = account.UserId,
                CreationDate = now,
                StatusId = (int)ClientRequestStatus.Draft
            };
            db.ClientRequests.Add(request);
        }
        if (needsReference) request.ReferenceNumber = await RequestReferenceGenerator.GenerateAsync(db, now);
        request.Title = Input.Title;
        request.Description = Input.Description;
        request.SiteAddress = Input.SiteAddress;
        request.PropertyType = Input.PropertyType;
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Your request draft has been saved. You can edit it or submit it when ready.";
        return RedirectToPage("./Details", new { id = request.Id });
    }
}
