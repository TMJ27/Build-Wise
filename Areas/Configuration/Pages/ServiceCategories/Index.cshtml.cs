using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.ServiceCategories;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    public IReadOnlyList<ServiceCategoryRowViewModel> Categories { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var categories = await db.ServiceCategories.AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        await db.ApplyServiceCategoryNumbersAsync(categories);

        var serviceCounts = await db.Services.AsNoTracking()
            .GroupBy(service => service.CategoryId)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Id, group => group.Count);

        Categories = categories.Select(category => new ServiceCategoryRowViewModel
        {
            Id = category.Id,
            CategoryNumber = category.CategoryNumber,
            Name = category.Name,
            IsActive = category.IsActive,
            ServiceCount = serviceCounts.GetValueOrDefault(category.Id),
            CreationDate = category.CreationDate
        }).ToList();
    }
}
