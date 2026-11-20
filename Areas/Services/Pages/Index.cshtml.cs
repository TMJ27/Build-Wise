using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Services.ViewModels;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Services.Pages;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public StatusFilter Status { get; set; } = StatusFilter.Active;

    public IReadOnlyList<ServiceIndexViewModel> Records { get; private set; } = [];
    public List<SelectListItem> Categories { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int ActiveCount { get; private set; }
    public int InactiveCount { get; private set; }
    public int AssessmentCount { get; private set; }
    public int CategoryCount { get; private set; }

    public async Task OnGetAsync()
    {
        Search = (Search ?? string.Empty).Trim();
        if (Search.Length > 120)
        {
            Search = Search[..120];
        }
        if (!Enum.IsDefined(Status) || ModelState[nameof(Status)]?.Errors.Count > 0)
        {
            Status = StatusFilter.Active;
            ModelState.Remove(nameof(Status));
        }

        // Cards describe the whole catalogue, independently of the table filters.
        TotalCount = await db.Services.CountAsync();
        ActiveCount = await db.Services.CountAsync(service => service.IsActive);
        InactiveCount = await db.Services.CountAsync(service => !service.IsActive);
        AssessmentCount = await db.Services.CountAsync(service => service.IsActive && service.RequireSiteAssessment);
        CategoryCount = await db.Services.Where(service => service.IsActive)
            .Select(service => service.CategoryId)
            .Distinct()
            .CountAsync();

        var inactiveLabel = ActiveStatus.Inactive.ToString();
        Categories = await db.ServiceCategories.AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new SelectListItem
            {
                Value = category.Id.ToString(),
                Text = category.IsActive ? category.Name : category.Name + " (" + inactiveLabel + ")"
            })
            .ToListAsync();

        var query = db.Services.AsNoTracking();

        if (Status != StatusFilter.All)
        {
            query = query.Where(service => service.IsActive == (Status == StatusFilter.Active));
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(service => service.CategoryId == CategoryId.Value);
        }

        if (Search.Length > 0)
        {
            var searchText = Search.ToUpper();
            query = query.Where(service => service.Name.ToUpper().Contains(searchText)
                || (service.Description != null && service.Description.ToUpper().Contains(searchText))
                || service.Category.Name.ToUpper().Contains(searchText));
        }

        var services = await query.Include(service => service.Category)
            .OrderBy(service => service.Name)
            .ToListAsync();

        // Apply whole-table display numbers after filtering, as in Deporess.
        await db.ApplyServiceNumbersAsync(services);

        Records = services
            .Select(service => new ServiceIndexViewModel
            {
                Id = service.Id,
                ServiceNumber = service.ServiceNumber,
                Name = service.Name,
                Description = service.Description,
                CategoryId = service.CategoryId,
                CategoryName = service.Category.Name,
                RequireSiteAssessment = service.RequireSiteAssessment,
                IsActive = service.IsActive,
                CreationDate = service.CreationDate
            })
            .ToList();
    }
}
