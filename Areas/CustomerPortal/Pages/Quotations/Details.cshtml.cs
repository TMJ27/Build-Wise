using BuildWise.Data;
using System.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services.CustomerPortal;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace BuildWise.Web.Areas.CustomerPortal.Pages.Quotations;

public class DetailsModel(BuildWiseDbContext db, CustomerPortalService portal) : PageModel
{
    public QuotationRevision Revision { get; private set; } = null!;
    public string QuotationNumber { get; private set; } = string.Empty;
    public QuotationResponse? CustomerResponse { get; private set; }
    public bool IsExpired => Revision.ValidUntil.HasValue && Revision.ValidUntil.Value.Date < DateTime.Now.Date;

    [BindProperty]
    public QuotationResponseInputViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        if (!await LoadAsync(id, account)) return NotFound();
        Input.RevisionId = Revision.Id;
        Input.Version = VersionOf(Revision);
        return Page();
    }

    public Task<IActionResult> OnPostAcceptAsync(Guid id) => RespondAsync(id, QuotationResponseStatus.Accepted);
    public Task<IActionResult> OnPostDeclineAsync(Guid id) => RespondAsync(id, QuotationResponseStatus.Declined);

    private async Task<IActionResult> RespondAsync(Guid id, QuotationResponseStatus status)
    {
        var account = await portal.GetAccountAsync(User);
        if (account is null) return Challenge(CustomerPortalAuthentication.Scheme);
        ViewData["CustomerName"] = account.Client.Name;
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        if (!await LoadAsync(id, account)) return NotFound();
        if (Input.RevisionId != Revision.Id || Input.Version != VersionOf(Revision))
            ModelState.AddModelError(string.Empty, "The quotation changed. Reopen it and review the latest approved revision.");
        if (CustomerResponse is not null)
            ModelState.AddModelError(string.Empty, "A response has already been recorded for this revision.");
        if (!Input.Confirm)
            ModelState.AddModelError("Input.Confirm", "Confirm that you reviewed this revision before responding.");
        if (status == QuotationResponseStatus.Accepted && IsExpired)
            ModelState.AddModelError(string.Empty, "This quotation has expired. Contact the team for an updated quotation.");
        var reason = Input.DeclineReason?.Trim();
        if (status == QuotationResponseStatus.Declined && string.IsNullOrWhiteSpace(reason))
            ModelState.AddModelError("Input.DeclineReason", "Enter a reason for declining the quotation.");
        if (reason?.Length > 2000)
            ModelState.AddModelError("Input.DeclineReason", "The reason must not exceed 2000 characters.");
        if (!ModelState.IsValid) return Page();
        db.QuotationResponses.Add(new QuotationResponse
        {
            Id = Guid.NewGuid(), QuotationRevisionId = Revision.Id, CustomerAccountId = account.Id,
            StatusId = (int)status, Reason = status == QuotationResponseStatus.Declined ? reason : null,
            CustomerNameSnapshot = account.Client.Name, CustomerEmailSnapshot = account.Email,
            CreationDate = DateTime.Now
        });
        try
        {
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (transaction is not null) await transaction.RollbackAsync();
            TempData["SuccessMessage"] = "A response has already been recorded for this revision.";
            return RedirectToPage(new { id });
        }
        TempData["SuccessMessage"] = status == QuotationResponseStatus.Accepted
            ? "You accepted this quotation." : "You declined this quotation. Your reason has been recorded.";
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(Guid id, CustomerAccount account)
    {
        var revision = await portal.Quotations(account).Include(revision => revision.Quotation)
            .Include(revision => revision.QuotationItems).SingleOrDefaultAsync(revision => revision.QuotationId == id);
        if (revision is null) return false;
        Revision = revision;
        var quotation = await db.Quotations.AsNoTracking().SingleAsync(item => item.Id == id);
        await db.ApplyQuotationNumberAsync(quotation);
        QuotationNumber = quotation.GeneratedQuotationNumber;
        CustomerResponse = await db.QuotationResponses.AsNoTracking()
            .SingleOrDefaultAsync(response => response.QuotationRevisionId == revision.Id);
        return true;
    }

    private static string VersionOf(QuotationRevision revision)
        => (revision.LastUpdate ?? revision.CreationDate).Ticks.ToString();
}
