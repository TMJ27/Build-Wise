using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.Areas.Services.ViewModels;

namespace BuildWise.Web.Areas.Services.Pages;

public class EditModel(CatalogueWorkspace catalogue) : PageModel
{
    [BindProperty]
    public ServiceInputViewModel Input { get; set; } = new();

    public CatalogueService? Existing { get; private set; }
    public IReadOnlyList<CatalogueService> AvailableDependencies { get; private set; } = [];

    public IActionResult OnGet(Guid? id)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        if (Existing is not null)
        {
            Input = new ServiceInputViewModel
            {
                Code = Existing.Code,
                Name = Existing.Name,
                Category = Existing.Category,
                Unit = Existing.Unit,
                Description = Existing.Description,
                IsActive = Existing.IsActive,
                RequiresSiteAssessment = Existing.RequiresSiteAssessment,
                Inclusions = string.Join(Environment.NewLine, Existing.Inclusions),
                Exclusions = string.Join(Environment.NewLine, Existing.Exclusions),
                Questions = string.Join(Environment.NewLine, Existing.Questions),
                Stages = string.Join(Environment.NewLine, Existing.Stages),
                DependencyIds = Existing.Dependencies.ToList()
            };
        }

        return Page();
    }

    public IActionResult OnPost(Guid? id)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        if (!CatalogueWorkspace.Categories.Contains(Input.Category))
        {
            ModelState.AddModelError("Input.Category", "Choose a service category.");
        }

        if (!CatalogueWorkspace.Units.Contains(Input.Unit))
        {
            ModelState.AddModelError("Input.Unit", "Choose a measurement unit.");
        }

        var inclusions = ReadLines(Input.Inclusions, "Inclusions");
        var exclusions = ReadLines(Input.Exclusions, "Exclusions");
        var questions = ReadLines(Input.Questions, "Questions");
        var stages = ReadLines(Input.Stages, "Stages");

        if (Input.DependencyIds.Any(dependencyId => AvailableDependencies.All(service => service.Id != dependencyId)))
        {
            ModelState.AddModelError(string.Empty, "Choose a dependency from the catalogue.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var service = new CatalogueService(
            id ?? Guid.NewGuid(),
            Input.Code.Trim().ToUpperInvariant(),
            Input.Name.Trim(),
            Input.Category,
            Input.Unit,
            Input.Description.Trim(),
            Input.RequiresSiteAssessment,
            Input.IsActive,
            inclusions,
            exclusions,
            questions,
            stages,
            Array.AsReadOnly(Input.DependencyIds.Distinct().ToArray()));

        var result = catalogue.Save(service);

        if (!result.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        TempData["CatalogueMessage"] = Existing is null ? "Service added to your catalogue." : "Service details updated.";
        return RedirectToPage("Details", new { id = result.Id });
    }

    private bool Load(Guid? id)
    {
        Existing = id.HasValue ? catalogue.Find(id.Value) : null;
        AvailableDependencies = catalogue.All.Where(service => service.Id != id).ToArray();
        return !id.HasValue || Existing is not null;
    }

    private IReadOnlyList<string> ReadLines(string? text, string field)
    {
        var lines = (text ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length > 30 || lines.Any(line => line.Length > 500))
        {
            ModelState.AddModelError($"Input.{field}", "Use up to 30 lines, with no more than 500 characters per line.");
        }

        return Array.AsReadOnly(lines);
    }
}
