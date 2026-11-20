using System.Data;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.Quotations;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class DetailsModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    public QuotationDocumentViewModel Document { get; private set; } = null!;
    public QuotationResponse? CustomerResponse { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? revisionId)
    {
        var document = await QuotationDocumentQueries.LoadAsync(db, id, revisionId);
        if (document is null) return NotFound();
        Document = document;
        CustomerResponse = await db.QuotationResponses.AsNoTracking()
            .SingleOrDefaultAsync(response => response.QuotationRevisionId == document.Revision.Id);
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id, Guid revisionId, string version, bool confirm)
    {
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Unauthorized();
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var latest = await db.QuotationRevisions.Include(item => item.QuotationItems)
            .Include(item => item.Quotation).ThenInclude(item => item.Request).ThenInclude(item => item.ScopeItems)
            .Where(item => item.QuotationId == id).OrderByDescending(item => item.RevisionNumber).FirstOrDefaultAsync();
        if (latest is null) return NotFound();
        if (latest.Id != revisionId || (latest.LastUpdate ?? latest.CreationDate).Ticks.ToString() != version)
            ModelState.AddModelError(string.Empty, "This quotation has changed. Reload it before approving.");
        if (latest.StatusId != (int)QuotationStatus.Draft)
            ModelState.AddModelError(string.Empty, "Only the latest draft can be approved.");
        if (!confirm) ModelState.AddModelError(string.Empty, "Confirm that you have reviewed the work, amounts and terms.");
        if (!ScopeQueries.CanEdit(latest.Quotation.Request.StatusId))
            ModelState.AddModelError(string.Empty, "The request is closed or cancelled.");
        var scope = latest.Quotation.Request.ScopeItems;
        if (latest.QuotationItems.Count == 0 || scope.Count == 0 || scope.Any(item => item.ReviewStatusId != (int)ScopeReviewStatus.Reviewed)
            || latest.QuotationItems.Any(item => !scope.Any(source => source.Id == item.ScopeItemId)))
            ModelState.AddModelError(string.Empty, "Review and approve the request scope before approving this quotation.");
        if (latest.IncludesPrices && (string.IsNullOrWhiteSpace(latest.CurrencyCode)
            || latest.QuotationItems.Any(item => !item.LineAmount.HasValue || item.LineAmount < 0)
            || latest.Subtotal != latest.QuotationItems.Sum(item => item.LineAmount ?? 0)
            || latest.TaxAmount is null or < 0 || latest.Total != latest.Subtotal + latest.TaxAmount))
            ModelState.AddModelError(string.Empty, "The quotation amounts are incomplete or inconsistent. Edit the draft before approving.");
        if (!ModelState.IsValid)
        {
            Document = (await QuotationDocumentQueries.LoadAsync(db, id))!;
            return Page();
        }
        var now = DateTime.Now;
        latest.StatusId = (int)QuotationStatus.Approved;
        latest.ApprovedByUserId = userId.Value;
        latest.ApprovalDate = now;
        latest.LastUpdate = now;
        latest.ApprovedDocumentJson = JsonSerializer.Serialize(new
        {
            latest.RevisionNumber, latest.Title, latest.CompanyNameSnapshot, latest.CompanyEmailSnapshot,
            latest.CompanyPhoneSnapshot, latest.CompanyAddressSnapshot, latest.CompanyRegistrationNumberSnapshot,
            latest.CompanyTaxNumberSnapshot, latest.CompanyLogoStorageKeySnapshot, latest.CustomerNameSnapshot,
            latest.CustomerEmailSnapshot, latest.CustomerPhoneSnapshot, latest.CustomerAddressSnapshot,
            latest.SiteAddressSnapshot, latest.RequestDescriptionSnapshot, latest.ClarificationsSnapshotJson,
            latest.IssueDate, latest.ValidUntil, latest.Terms, latest.Exclusions, latest.IncludesPrices,
            latest.CurrencyCode, latest.Subtotal, latest.TaxAmount, latest.Total,
            latest.ApprovedByUserId, latest.ApprovalDate,
            Items = latest.QuotationItems.OrderBy(item => item.Title).Select(item => new
            {
                item.ScopeItemId, item.Title, item.Description, item.Quantity, item.UnitCodeSnapshot,
                item.ServiceCodeSnapshot, item.LineAmount, item.RequiresSiteAssessment,
                item.AssessmentNotesSnapshot, item.WorkStagesSnapshotJson, item.DependenciesSnapshotJson
            })
        });
        latest.Quotation.Request.StatusId = (int)ClientRequestStatus.Quoted;
        await db.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        TempData["SuccessMessage"] = "Quotation approved. You can now print or save the document as PDF.";
        return RedirectToPage(new { id });
    }
}
