using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.ServiceCategories;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ServiceCategoryDetailsViewModel CategoryDetails { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var category = await db.ServiceCategories.AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == id);

        if (category is null)
        {
            return NotFound();
        }

        await db.ApplyServiceCategoryNumberAsync(category);

        CategoryDetails = new ServiceCategoryDetailsViewModel
        {
            Id = category.Id,
            CategoryNumber = category.CategoryNumber,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            ServiceCount = await db.Services.CountAsync(service => service.CategoryId == id),
            CreationDate = category.CreationDate,
            LastUpdated = category.LastUpdated
        };

        return Page();
    }
}
