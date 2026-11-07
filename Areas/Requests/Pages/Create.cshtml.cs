using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Requests.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Requests.Pages;

public class CreateModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty] public RequestInputViewModel Input { get; set; } = new();
    public List<SelectListItem> Customers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? customerId, Guid? serviceId)
    {
        if (customerId.HasValue)
        {
            var customer = await db.Clients.AsNoTracking().SingleOrDefaultAsync(client => client.Id == customerId && client.IsActive);
            if (customer is null) return NotFound();
            Input.ClientId = customer.Id;
            Input.SiteAddress = customer.Address;
        }
        if (serviceId.HasValue)
        {
            var service = await db.Services.AsNoTracking().SingleOrDefaultAsync(service => service.Id == serviceId && service.IsActive);
            if (service is null) return NotFound();
            Input.Title = service.Name.Length > 50 ? service.Name[..50] : service.Name;
            Input.Description = service.Description ?? string.Empty;
        }
        await LoadCustomersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();
        Input.Title = (Input.Title ?? string.Empty).Trim();
        Input.SiteAddress = (Input.SiteAddress ?? string.Empty).Trim();
        Input.Description = (Input.Description ?? string.Empty).Trim();
        Input.ReferenceNumber = Clean(Input.ReferenceNumber);
        Input.PropertyType = Clean(Input.PropertyType);
        if (!await db.Clients.AnyAsync(client => client.Id == Input.ClientId && client.IsActive))
            ModelState.AddModelError("Input.ClientId", "Choose an active customer.");
        if (Input.Title.Length is < 1 or > 50) ModelState.AddModelError("Input.Title", "Enter a title of 1 to 50 characters.");
        if (Input.SiteAddress.Length is < 1 or > 50) ModelState.AddModelError("Input.SiteAddress", "Enter a site address of 1 to 50 characters.");
        if (Input.Description.Length is < 1 or > 3000) ModelState.AddModelError("Input.Description", "Enter a description of 1 to 3000 characters.");
        if (Input.PropertyType?.Length > 50) ModelState.AddModelError("Input.PropertyType", "Property type must not exceed 50 characters.");
        if (Input.ReferenceNumber?.Length > 50) ModelState.AddModelError("Input.ReferenceNumber", "Reference must not exceed 50 characters.");
        if (Input.ReferenceNumber is not null)
        {
            var reference = Input.ReferenceNumber.ToUpper();
            if (await db.ClientRequests.AnyAsync(request => request.ReferenceNumber != null && request.ReferenceNumber.ToUpper() == reference))
                ModelState.AddModelError("Input.ReferenceNumber", "This request reference is already in use.");
        }
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return Page();
        }
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var request = new ClientRequest
        {
            Id = id, ClientId = Input.ClientId!.Value, Title = Input.Title, Description = Input.Description,
            SiteAddress = Input.SiteAddress, PropertyType = Input.PropertyType,
            ReferenceNumber = Input.ReferenceNumber ?? $"REQ-{now:yyyyMMdd}-{id:N}",
            StatusId = (int)ClientRequestStatus.Open, CreatorId = userId.Value, CreationDate = now
        };
        db.ClientRequests.Add(request);
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Request created successfully. You can now prepare its scope.";
        return RedirectToPage("./Details", new { id = request.Id });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task LoadCustomersAsync() => Customers = await db.Clients.AsNoTracking().Where(client => client.IsActive)
        .OrderBy(client => client.Name).Select(client => new SelectListItem { Value = client.Id.ToString(), Text = client.Name }).ToListAsync();
}
