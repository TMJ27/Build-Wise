using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Scopes.ViewModels;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages;

[EnableRateLimiting("GeminiScopeDraft")]
public class DetailsModel(
    BuildWiseDbContext db,
    GeminiScopeDraftService gemini,
    ICurrentUserService currentUser,
    IConfiguration configuration,
    ILogger<DetailsModel> logger) : PageModel
{
    [BindProperty]
    public List<GeminiScopeDraftViewModel> DraftItems { get; set; } = [];

    public ScopeViewModel Scope { get; set; } = new();
    public IReadOnlyList<ScopeItemDetailsViewModel> Items { get; set; } = [];
    public List<SelectListItem> ActiveServices { get; private set; } = [];
    public bool GeminiConfigured { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!await LoadPageAsync(id)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostGenerateAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await LoadPageAsync(id)) return NotFound();
        if (!Scope.CanEdit) return StatusCode(403);

        try
        {
            var request = await db.ClientRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (request is null) return NotFound();
            var services = await db.Services.AsNoTracking().Where(service => service.IsActive)
                .OrderBy(service => service.Name).ToListAsync(cancellationToken);
            var suggestions = await gemini.GenerateAsync(request, services, cancellationToken);
            DraftItems = suggestions.Select(suggestion => new GeminiScopeDraftViewModel
            {
                Selected = false,
                ServiceId = suggestion.ServiceId,
                Title = suggestion.Title,
                Description = suggestion.Description,
                Quantity = suggestion.Quantity,
                UnitCode = suggestion.UnitCode,
                RequireAssessment = suggestion.RequireAssessment,
                Clauses = suggestion.Clauses.Select(clause => new GeminiScopeClauseDraftViewModel
                {
                    Type = clause.Type,
                    Wording = clause.Wording
                }).ToList()
            }).ToList();
            if (DraftItems.Count == 0)
                ModelState.AddModelError(string.Empty, "AI could not find suitable scope items to suggest for this request.");
        }
        catch (ScopeDraftUnavailableException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not connect to Gemini while generating a scope draft for request {RequestId}.", id);
            ModelState.AddModelError(string.Empty, "AI scope preparation is temporarily unavailable. Try again shortly.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scope draft generation failed for request {RequestId}.", id);
            ModelState.AddModelError(string.Empty, "The scope draft could not be generated. Try again.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveDraftAsync(Guid id)
    {
        if (!await LoadPageAsync(id)) return NotFound();
        if (!Scope.CanEdit) return StatusCode(403);

        var selected = DraftItems.Where(item => item.Selected).ToList();
        if (selected.Count is < 1 or > 12)
            ModelState.AddModelError(string.Empty, "Select between 1 and 12 scope items to add.");

        foreach (var (draft, index) in selected.Select((item, index) => (item, index)))
        {
            draft.Title = (draft.Title ?? string.Empty).Trim();
            draft.Description = (draft.Description ?? string.Empty).Trim();
            draft.UnitCode = Clean(draft.UnitCode);
            if (draft.Title.Length is < 1 or > 50)
                ModelState.AddModelError($"DraftItems[{index}].Title", "Titles must be 1 to 50 characters.");
            if (draft.Description.Length is < 1 or > 4000)
                ModelState.AddModelError($"DraftItems[{index}].Description", "Descriptions must be 1 to 4000 characters.");
            if (draft.Quantity is <= 0)
                ModelState.AddModelError($"DraftItems[{index}].Quantity", "Quantity must be a positive whole number.");
            if (draft.UnitCode?.Length > 50)
                ModelState.AddModelError($"DraftItems[{index}].UnitCode", "The unit must not exceed 50 characters.");
            foreach (var clause in draft.Clauses)
            {
                clause.Wording = Clean(clause.Wording);
                if (!Enum.IsDefined(clause.Type) || clause.Wording?.Length > 4000)
                    ModelState.AddModelError(string.Empty, "Each clause must have a supported type and no more than 4,000 characters.");
            }
        }

        if (!ModelState.IsValid)
            return Page();

        var serviceIds = selected.Select(item => item.ServiceId).Distinct().ToList();
        var services = await db.Services.Include(service => service.ServiceUnits).ThenInclude(link => link.Unit)
            .Where(service => service.IsActive && serviceIds.Contains(service.Id)).ToDictionaryAsync(service => service.Id);
        if (services.Count != serviceIds.Count)
        {
            ModelState.AddModelError(string.Empty, "One of the selected services is no longer active. Generate a new draft and review it again.");
            return Page();
        }

        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();

        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var now = DateTime.UtcNow;
        foreach (var draft in selected)
        {
            var service = services[draft.ServiceId];
            service.ServiceSequence = await db.GetServiceSequenceAsync(service);
            var unitCode = draft.UnitCode ?? service.ServiceUnits
                .Where(link => link.IsDefault == true && link.Unit?.IsActive == true)
                .OrderBy(link => link.Id).Select(link => link.Unit!.Code).FirstOrDefault();
            var item = new ScopeItem
            {
                Id = Guid.NewGuid(),
                RequestId = id,
                ServiceId = service.Id,
                ServiceCodeSnapshot = service.ServiceNumber,
                Title = draft.Title,
                Description = draft.Description,
                Quantity = draft.Quantity,
                UnitCodeSnapshot = unitCode,
                RequireAssessment = draft.RequireAssessment || service.RequireSiteAssessment,
                ReviewStatusId = (int)ScopeReviewStatus.Pending,
                ReviewedByUserId = userId.Value
            };
            db.ScopeItems.Add(item);

            foreach (var clause in draft.Clauses.Where(clause => !string.IsNullOrWhiteSpace(clause.Wording)))
            {
                db.ScopeClauses.Add(new ScopeClause
                {
                    Id = Guid.NewGuid(),
                    ScopeItem = item,
                    ClauseTypeId = (int)clause.Type,
                    Description = clause.Wording!.Trim(),
                    CreationDate = now,
                    CreatorId = userId.Value
                });
            }
        }

        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = $"Added {selected.Count} AI-assisted scope item(s). Review them before preparing a quotation.";
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadPageAsync(Guid id)
    {
        var scope = await ScopeQueries.Requests(db).SingleOrDefaultAsync(request => request.RequestId == id);
        if (scope is null) return false;
        Scope = scope;
        var items = await db.ScopeItems.AsNoTracking().Include(item => item.Service)
            .Include(item => item.Request).Include(item => item.ReviewedByUser)
            .Where(item => item.RequestId == id).OrderBy(item => item.Title).ThenBy(item => item.Id).ToListAsync();
        Items = items.Select(ScopeQueries.MapItem).ToList();
        ActiveServices = await db.Services.AsNoTracking().Where(service => service.IsActive)
            .OrderBy(service => service.Name)
            .Select(service => new SelectListItem(service.Name, service.Id.ToString())).ToListAsync();
        GeminiConfigured = !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"])
            && !string.IsNullOrWhiteSpace(configuration["Gemini:Model"]);
        return true;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
