using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.ServiceCategories;

public class UpsertModel(BuildWiseDbContext db) : PageModel
{
    [BindProperty]
    public ServiceCategoryUpsertViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return Page();
        }

        var category = await db.ServiceCategories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id.Value);

        if (category is null)
        {
            return NotFound();
        }

        Input.Id = category.Id;
        Input.Name = category.Name;
        Input.Description = category.Description;
        Input.IsActive = category.IsActive;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (id != Input.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Input.Name = (Input.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Enter the category name.");
            return Page();
        }

        var normalizedName = Input.Name.ToUpper();
        if (await db.ServiceCategories.AnyAsync(category => category.Name.ToUpper() == normalizedName && category.Id != id))
        {
            ModelState.AddModelError("Input.Name", "A service category with this name already exists.");
            return Page();
        }

        var isNew = !id.HasValue;
        ServiceCategory category;

        if (id.HasValue)
        {
            var existing = await db.ServiceCategories.FirstOrDefaultAsync(category => category.Id == id.Value);
            if (existing is null)
            {
                return NotFound();
            }

            category = existing;
            category.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            category = new ServiceCategory
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.UtcNow
            };
            db.ServiceCategories.Add(category);
        }

        category.Name = Input.Name;
        category.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        category.IsActive = Input.IsActive;

        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = isNew
            ? "Service category created successfully."
            : "Service category updated successfully.";

        return RedirectToPage("./Details", new { id = category.Id });
    }
}
