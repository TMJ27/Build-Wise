using BuildWise.Web.Areas.Scopes.ViewModels;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Scopes.Pages.Items;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public ScopeItemUpsertViewModel Input { get; set; } = new();

    public List<SelectListItem> Requests { get; set; } = [];
    public List<SelectListItem> Services { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? id, Guid? requestId)
    {
        if (id.HasValue)
        {
            var item = await db.ScopeItems.AsNoTracking().Include(item => item.Request).SingleOrDefaultAsync(item => item.Id == id);
            if (item is null || (requestId.HasValue && item.RequestId != requestId)) return NotFound();
            if (!ScopeQueries.CanEdit(item.Request.StatusId)) return StatusCode(403);
            Input = new ScopeItemUpsertViewModel
            {
                Id = item.Id, RequestId = item.RequestId, ServiceId = item.ServiceId,
                Title = item.Title, Description = item.Description, Quantity = item.Quantity,
                UnitCodeSnapshot = item.UnitCodeSnapshot, RequireAssessment = item.RequireAssessment,
                AssessmentNotes = ScopeQueries.ReadNotes(item.AssessmentNotesJson),
                MarkReviewed = item.ReviewStatusId == (int)ScopeReviewStatus.Reviewed
            };
        }
        else if (requestId.HasValue)
        {
            var request = await db.ClientRequests.AsNoTracking().SingleOrDefaultAsync(request => request.Id == requestId);
            if (request is null) return NotFound();
            if (!ScopeQueries.CanEdit(request.StatusId)) return StatusCode(403);
            Input.RequestId = request.Id;
        }
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id, Guid? requestId)
    {
        if (id != Input.Id || (requestId.HasValue && requestId != Input.RequestId)) return BadRequest();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();
        var item = id.HasValue ? await db.ScopeItems.Include(item => item.Request).SingleOrDefaultAsync(item => item.Id == id) : null;
        if (id.HasValue && item is null) return NotFound();
        // A scope item cannot move to another request or service once created.
        // Its clauses, dependencies, stages and existing quotation snapshots remain intact.
        if (item is not null && (Input.RequestId != item.RequestId || Input.ServiceId != item.ServiceId)) return BadRequest();
        var request = await db.ClientRequests.SingleOrDefaultAsync(request => request.Id == Input.RequestId);
        if (request is null)
            ModelState.AddModelError("Input.RequestId", "Select an existing request.");
        else if (!ScopeQueries.CanEdit(request.StatusId))
            ModelState.AddModelError("Input.RequestId", "Closed or cancelled requests cannot be changed.");
        var service = await db.Services.Include(service => service.ServiceUnits).ThenInclude(link => link.Unit)
            .SingleOrDefaultAsync(service => service.Id == Input.ServiceId);
        if (service is null || (!service.IsActive && service.Id != item?.ServiceId))
            ModelState.AddModelError("Input.ServiceId", "Select an active service.");

        Input.Title = (Input.Title ?? string.Empty).Trim();
        Input.Description = Clean(Input.Description);
        Input.UnitCodeSnapshot = Clean(Input.UnitCodeSnapshot);
        Input.AssessmentNotes = Clean(Input.AssessmentNotes);
        if (Input.Title.Length is < 1 or > 50)
            ModelState.AddModelError("Input.Title", "Enter a title of 1 to 50 characters.");
        if (Input.UnitCodeSnapshot?.Length > 50)
            ModelState.AddModelError("Input.UnitCodeSnapshot", "The unit must not exceed 50 characters.");
        if (Input.Quantity is <= 0)
            ModelState.AddModelError("Input.Quantity", "Quantity must be a positive whole number.");
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(item?.ServiceId);
            return Page();
        }

        var now = DateTime.UtcNow;
        if (item is null)
        {
            service!.ServiceSequence = await db.GetServiceSequenceAsync(service);
            item = new ScopeItem
            {
                Id = Guid.NewGuid(), RequestId = request!.Id, ServiceId = service.Id,
                ServiceCodeSnapshot = service.ServiceNumber
            };
            if (Input.UnitCodeSnapshot is null)
            {
                Input.UnitCodeSnapshot = service.ServiceUnits.Where(link => link.IsDefault == true && link.Unit?.IsActive == true)
                    .OrderBy(link => link.Id).Select(link => link.Unit!.Code).FirstOrDefault();
            }
            db.ScopeItems.Add(item);
        }
        item.Title = Input.Title;
        item.Description = Input.Description;
        item.Quantity = Input.Quantity;
        item.UnitCodeSnapshot = Input.UnitCodeSnapshot;
        item.RequireAssessment = Input.RequireAssessment || service!.RequireSiteAssessment;
        // Leave existing legacy JSON untouched if the visible notes did not change.
        if (ScopeQueries.ReadNotes(item.AssessmentNotesJson) != Input.AssessmentNotes)
            item.AssessmentNotesJson = Input.AssessmentNotes is null ? null : JsonSerializer.Serialize(new { notes = Input.AssessmentNotes });
        item.ReviewStatusId = (int)(Input.MarkReviewed ? ScopeReviewStatus.Reviewed : ScopeReviewStatus.Pending);
        // The schema requires ReviewedByUserId even while pending; ReviewedOn is set only on review.
        item.ReviewedByUserId = userId.Value;
        item.ReviewedOn = Input.MarkReviewed ? now : null;
        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = id.HasValue ? "Scope item updated successfully." : "Scope item created successfully.";
        return RedirectToPage("./Details", new { id = item.Id, requestId = item.RequestId });
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task LoadOptionsAsync(Guid? existingServiceId = null)
    {
        Requests = await db.ClientRequests.AsNoTracking()
            .Where(request => request.StatusId != (int)ClientRequestStatus.Closed && request.StatusId != (int)ClientRequestStatus.cancelled)
            .OrderBy(request => request.Title).Select(request => new SelectListItem
            {
                Value = request.Id.ToString(), Text = (request.ReferenceNumber ?? "Request") + " · " + request.Client.Name + " · " + request.Title
            }).ToListAsync();
        Services = await db.Services.AsNoTracking().Where(service => service.IsActive || service.Id == existingServiceId
                || (Input.Id.HasValue && service.Id == Input.ServiceId))
            .OrderBy(service => service.Name).Select(service => new SelectListItem
            {
                Value = service.Id.ToString(), Text = service.Name + (service.IsActive ? "" : " (Inactive)")
            }).ToListAsync();
    }
}
