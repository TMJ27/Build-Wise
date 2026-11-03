using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Requests;
public class CreateModel(
    RequestWorkspace workspace,
    CustomerWorkspace customers,
    CatalogueWorkspace catalogue) : PageModel
{
    public IReadOnlyList<string> Categories => CatalogueWorkspace.Categories;
    [BindProperty] public RequestInput Input { get; set; } = new();
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
    public sealed class RequestInput
    {
        [Required, StringLength(120)] public string Title { get; set; } = "";
        [Required, StringLength(100)] public string Customer { get; set; } = "";
        [EmailAddress, StringLength(200)] public string? Email { get; set; }
        [Required] public string Category { get; set; } = "";
        [Required, StringLength(200)] public string Location { get; set; } = "";
        [Required, StringLength(3000), MinLength(20, ErrorMessage = "Add at least 20 characters about the work required.")] public string Description { get; set; } = "";
    }
}
