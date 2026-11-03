using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Customers;

public class EditModel(CustomerWorkspace customers) : PageModel
{
    [BindProperty]
    public CustomerInput Input { get; set; } = new();

    public CustomerEntry? Existing { get; private set; }

    public IActionResult OnGet(int? id)
    {
        Existing = id.HasValue ? customers.Find(id.Value) : null;

        if (id.HasValue && Existing is null)
        {
            return NotFound();
        }

        if (Existing is not null)
        {
            Input = new CustomerInput
            {
                Name = Existing.Name,
                Type = Existing.Type,
                Email = Existing.Email,
                Phone = Existing.Phone,
                Address = Existing.Address,
                Notes = Existing.Notes
            };
        }

        return Page();
    }

    public IActionResult OnPost(int? id)
    {
        Existing = id.HasValue ? customers.Find(id.Value) : null;

        if (id.HasValue && Existing is null)
        {
            return NotFound();
        }

        if (Input.Type is not ("Individual" or "Business"))
        {
            ModelState.AddModelError("Input.Type", "Choose a customer type.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var customerId = customers.Save(
            id,
            Input.Name,
            Input.Type,
            Input.Email ?? "",
            Input.Phone ?? "",
            Input.Address ?? "",
            Input.Notes ?? "");

        if (!customerId.HasValue)
        {
            ModelState.AddModelError("Input.Name", "A customer with this name already exists. Open their profile instead.");
            return Page();
        }

        TempData["CustomerMessage"] = Existing is null ? "Customer added successfully." : "Customer details updated.";
        return RedirectToPage("Details", new { id = customerId });
    }

    public sealed class CustomerInput
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "Individual";

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Phone, StringLength(40)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }
    }
}
