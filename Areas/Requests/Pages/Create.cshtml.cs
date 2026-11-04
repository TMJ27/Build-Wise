using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.Areas.Requests.ViewModels;

namespace BuildWise.Web.Areas.Requests.Pages;
public class CreateModel(
    RequestWorkspace workspace,
    CustomerWorkspace customers,
    CatalogueWorkspace catalogue) : PageModel
{
    public IReadOnlyList<string> Categories => CatalogueWorkspace.Categories;
    [BindProperty] public RequestInputViewModel Input { get; set; } = new();
    public void OnGet(int? customerId, Guid? serviceId)
    {
        var customer = customerId.HasValue ? customers.Find(customerId.Value) : null;

        if (customer is not null)
        {
            Input.Customer = customer.Name;
            Input.Email = customer.Email;
            Input.Location = customer.Address;
        }

        var service = serviceId.HasValue ? catalogue.Find(serviceId.Value) : null;

        if (service is { IsActive: true })
        {
            Input.Category = service.Category;
            Input.Title = service.Name;
        }
    }
    public IActionResult OnPost()
    {
        if (!Categories.Contains(Input.Category)) ModelState.AddModelError("Input.Category", "Choose a service category.");
        if (!ModelState.IsValid) return Page();
        var id = workspace.Create(Input.Title, Input.Customer, Input.Category, Input.Location, Input.Email ?? "", Input.Description);
        TempData["RequestCreated"] = $"Request REQ-{id} created successfully.";
        return RedirectToPage("Details", new { id });
    }
}
