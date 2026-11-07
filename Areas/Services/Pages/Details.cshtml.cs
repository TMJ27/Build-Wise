using BuildWise.Data;
using BuildWise.Web.Areas.Services.ViewModels;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Services.Pages;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public ServiceDetailsViewModel ServiceDetails { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var service = await db.Services.AsNoTracking()
            .Include(service => service.Category)
            .Include(service => service.Creator)
            .FirstOrDefaultAsync(service => service.Id == id);

        if (service is null)
        {
            return NotFound();
        }

        await db.ApplyServiceNumberAsync(service);

        var creatorName = $"{service.Creator.Name} {service.Creator.Surname}".Trim();
        ServiceDetails = new ServiceDetailsViewModel
        {
            Id = service.Id,
            ServiceNumber = service.ServiceNumber,
            Name = service.Name,
            Description = service.Description,
            CategoryId = service.CategoryId,
            CategoryName = service.Category.Name,
            CategoryIsActive = service.Category.IsActive,
            RequireSiteAssessment = service.RequireSiteAssessment,
            IsActive = service.IsActive,
            CreatorName = string.IsNullOrWhiteSpace(creatorName) ? service.Creator.Email : creatorName,
            CreationDate = service.CreationDate,
            LastUpdated = service.LastUpdated
        };

        return Page();
    }

    public async Task<IActionResult> OnPostToogleAsync(Guid id)
    {
        var service = await db.Services.FirstOrDefaultAsync(service => service.Id == id);
        if (service is null)
        {
            return NotFound();
        }

        service.IsActive = !service.IsActive;
        service.LastUpdated = DateTime.Now;
        await db.SaveChangesAsync();

        TempData["SuccessMessage"] = service.IsActive
            ? "Service activated successfully."
            : "Service deactivated successfully.";

        return RedirectToPage("./Details", new { area="Services", id });
    }
}