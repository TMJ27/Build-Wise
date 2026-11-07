using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Requests;

public class CreateModel(BuildWiseDbContext db, CustomerPortalService portal) : PageModel
{
    [BindProperty] public CustomerRequestInputViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        Input.SiteAddress = account.Client.Address;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        Input.Title = (Input.Title ?? string.Empty).Trim();
        Input.SiteAddress = (Input.SiteAddress ?? string.Empty).Trim();
        Input.Description = (Input.Description ?? string.Empty).Trim();
        Input.PropertyType = string.IsNullOrWhiteSpace(Input.PropertyType) ? null : Input.PropertyType.Trim();
        if (Input.Title.Length is < 1 or > 50) ModelState.AddModelError("Input.Title", "Enter a title of 1 to 50 characters.");
        if (Input.SiteAddress.Length is < 1 or > 50) ModelState.AddModelError("Input.SiteAddress", "Enter a site address of 1 to 50 characters.");
        if (Input.Description.Length is < 1 or > 3000) ModelState.AddModelError("Input.Description", "Enter a description of 1 to 3000 characters.");
        if (Input.PropertyType?.Length > 50) ModelState.AddModelError("Input.PropertyType", "Property type must not exceed 50 characters.");
        if (!ModelState.IsValid) return Page();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.ClientRequests.Add(new ClientRequest
        {
            Id = id, ClientId = account.ClientId, CreatorId = account.UserId, Title = Input.Title,
            Description = Input.Description, SiteAddress = Input.SiteAddress, PropertyType = Input.PropertyType,
            ReferenceNumber = $"REQ-{now:yyyyMMdd}-{id:N}", CreationDate = now, StatusId = (int)ClientRequestStatus.Open
        });
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Your request has been submitted.";
        return RedirectToPage("./Details", new { id });
    }
}
