using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Services;

public class DetailsModel(CatalogueWorkspace catalogue) : PageModel
{
    public CatalogueService Service { get; private set; } = null!;
    public IReadOnlyList<CatalogueService> Dependencies { get; private set; } = [];

    public IActionResult OnGet(Guid id)
    {
        var service = catalogue.Find(id);

        if (service is null)
        {
            return NotFound();
        }

        Service = service;
        Dependencies = catalogue.All.Where(entry => service.Dependencies.Contains(entry.Id)).ToArray();
        return Page();
    }

    public IActionResult OnPostToggle(Guid id)
    {
        var service = catalogue.Find(id);

        if (service is null || !catalogue.SetActive(id, !service.IsActive))
        {
            return NotFound();
        }

        TempData["CatalogueMessage"] = service.IsActive
            ? "Service archived. Its details are retained for reference."
            : "Service activated and available in your catalogue.";

        return RedirectToPage(new { id });
    }
}
