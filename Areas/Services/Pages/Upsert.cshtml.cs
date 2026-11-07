using BuildWise.Web.Areas.Services.ViewModels;
using BuildWise.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BuildWise.Lib.Users;
using BuildWise.Lib.Enums.Statuses;

namespace BuildWise.Web.Areas.Services.Pages;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService CurrentUser) : PageModel
{
    [BindProperty]
    public ServiceUpsertViewModel Input { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = [];

    public Service? Service { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (id.HasValue)
        {
            var service = await db.Services.AsNoTracking().FirstOrDefaultAsync(service => service.Id == id.Value);
            if (service is null)
            {
                return NotFound();
            }

            Input.Id = service.Id;
            Input.Name = service.Name;
            Input.CategoryId = service.CategoryId;
            Input.Description = service.Description;
            Input.RequireSiteAssessment = service.RequireSiteAssessment;
            Input.IsActive = service.IsActive;
        }

        await LoadCategoriesAsync();
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
            await LoadCategoriesAsync();
            return Page();
        }

        Input.Name = (Input.Name ?? string.Empty).Trim();
        Input.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        if (Input.Name.Length == 0 || !Input.CategoryId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "Enter a service name and select a category.");
            await LoadCategoriesAsync();
            return Page();
        }

        Service = id.HasValue
            ? await db.Services.FirstOrDefaultAsync(service => service.Id == id.Value)
            : null;
        if (id.HasValue && Service is null)
        {
            return NotFound();
        }

        var existingCategoryId = Service?.CategoryId;
        if (!await db.ServiceCategories.AnyAsync(category => category.Id == Input.CategoryId.Value
                && (category.IsActive || category.Id == existingCategoryId)))
        {
            ModelState.AddModelError("Input.CategoryId", "Select an active service category.");
            await LoadCategoriesAsync();
            return Page();
        }

        var normalizedName = Input.Name.ToUpper();
        if (await db.Services.AnyAsync(service => service.Name.ToUpper() == normalizedName && service.CategoryId == Input.CategoryId && service.Id != id))
        {
            ModelState.AddModelError("Input.Name", "A service with this name already exists in the selected category.");
            await LoadCategoriesAsync();
            return Page();
        }

        var isNew = Service is null;
        if (Service is null)
        {
            var creatorId = await CurrentUser.GetUserIdAsync();
            if (!creatorId.HasValue)
            {
                return Unauthorized();
            }

            Service = new Service
            {
                Id = Guid.NewGuid(),
                CreatorId = creatorId.Value,
                CreationDate = DateTime.Now,
                Name = Input.Name,
                Description = Input.Description,
                RequireSiteAssessment = Input.RequireSiteAssessment,
                IsActive = Input.IsActive,
                CategoryId = Input.CategoryId.Value,
            };
            db.Services.Add(Service);
        }
        else
        {
            Service.Name = Input.Name;
            Service.Description = Input.Description;
            Service.RequireSiteAssessment = Input.RequireSiteAssessment;
            Service.IsActive = Input.IsActive;
            Service.CategoryId = Input.CategoryId.Value;
            Service.LastUpdated = DateTime.UtcNow;
            db.Services.Update(Service);

        }

        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = isNew
            ? "Service created successfully."
            : "Service updated successfully.";

        return RedirectToPage("./Details", new { area = "Services", id = Service.Id });
    }
    public async Task LoadCategoriesAsync()
    {
        var inactiveLabel = ActiveStatus.Inactive.ToString();
        Categories = await db.ServiceCategories.AsNoTracking()
            .Where(category => category.IsActive || category.Id == Input.CategoryId)
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.IsActive ? category.Name : category.Name + " (" + inactiveLabel + ")"
            })
            .ToListAsync();
    }
}
