using BuildWise.Web.Services;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class DetailsModel(
    QuotationWorkspace quotations,
    RequestWorkspace requests,
    GeminiQuotationReviewService reviewService,
    IConfiguration configuration,
    ILogger<DetailsModel> logger) : PageModel
{
    public QuotationEntry Entry { get; private set; } = null!;
    public RequestEntry SourceRequest { get; private set; } = null!;
    public IReadOnlyList<QuotationReviewFinding> ReviewFindings { get; private set; } = [];
    public bool ReviewConfigured { get; private set; }
    public bool ReviewCompleted { get; private set; }

    public IActionResult OnGet(int id)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        return Page();
    }

    public IActionResult OnPostApprove(int id, bool confirm)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        if (!confirm)
        {
            ModelState.AddModelError(string.Empty, "Confirm that you have reviewed the scope, prices and terms.");
            return Page();
        }

        if (quotations.Approve(id))
        {
            requests.SetStatus(Entry.Quotation.RequestId, "Quotation approved");
            TempData["QuotationMessage"] = "Quotation approved. The approved document is ready to print.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReviewAsync(int id, CancellationToken cancellationToken)
    {
        if (!Load(id)) return NotFound();
        if (Entry.Quotation.Status != "Draft") return BadRequest();

        try
        {
            ReviewFindings = await reviewService.ReviewAsync(new QuotationReviewSource(
                SourceRequest.Request.Title,
                SourceRequest.Description,
                Entry.Lines.Select(line => new QuotationReviewLine(line.Description, line.Quantity)).ToList(),
                Entry.Terms,
                Entry.Exclusions), cancellationToken);
            ReviewCompleted = true;
        }
        catch (QuotationReviewUnavailableException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not connect while reviewing quotation {QuotationId}.", id);
            ModelState.AddModelError(string.Empty, "Quotation review is temporarily unavailable. Try again shortly.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Quotation review failed for quotation {QuotationId}.", id);
            ModelState.AddModelError(string.Empty, "The quotation could not be reviewed. Try again.");
        }

        return Page();
    }

    private bool Load(int id)
    {
        var entry = quotations.Find(id);
        var request = entry is null ? null : requests.Find(entry.Quotation.RequestId);

        if (entry is null || request is null)
        {
            return false;
        }

        Entry = entry;
        SourceRequest = request;
        ReviewConfigured = !string.IsNullOrWhiteSpace(configuration["Gemini:ApiKey"])
            && !string.IsNullOrWhiteSpace(configuration["Gemini:Model"]);
        return true;
    }
}
