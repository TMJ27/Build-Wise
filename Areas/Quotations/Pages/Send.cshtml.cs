using System.Net;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.Email;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Quotations.Pages;

[RequestSizeLimit(12 * 1024 * 1024)]
public class SendModel(BuildWiseDbContext db, ICurrentUserService currentUser, IEmailSender sender) : PageModel
{
    public QuotationDocumentViewModel Document { get; private set; } = null!;

    [BindProperty]
    public SendQuotationInputViewModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? PdfAttachment { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, Guid revisionId)
    {
        if (!await LoadAsync(id, revisionId)) return NotFound();
        Input = new SendQuotationInputViewModel
        {
            RecipientEmail = Document.Revision.CustomerEmailSnapshot ?? string.Empty,
            Subject = $"Quotation {Document.Number} - Revision {Document.Revision.RevisionNumber}",
            Message = $"Dear {Document.Revision.CustomerNameSnapshot},\n\nPlease find your quotation attached.\n\nKind regards,\n{Document.Revision.CompanyNameSnapshot}",
            Version = Document.Version, SendId = Guid.NewGuid()
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, Guid revisionId)
    {
        if (!await LoadAsync(id, revisionId)) return NotFound();
        var userId = await currentUser.GetUserIdAsync();
        if (!userId.HasValue) return Forbid();
        if (Input.SendId == Guid.Empty) return BadRequest();
        if (Input.Version != Document.Version)
            ModelState.AddModelError(string.Empty, "The quotation changed. Reopen the send form before continuing.");
        if (PdfAttachment is null || PdfAttachment.Length < 5 || PdfAttachment.Length > 10 * 1024 * 1024)
            ModelState.AddModelError(string.Empty, "Generate a quotation PDF attachment of up to 10 MB before sending.");
        if (!ModelState.IsValid) return Page();
        try
        {
            using var attachment = new MemoryStream();
            await PdfAttachment!.CopyToAsync(attachment, HttpContext.RequestAborted);
            var pdf = attachment.ToArray();
            if (!pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            {
                ModelState.AddModelError(string.Empty, "The quotation attachment is not a PDF. Generate it again.");
                return Page();
            }
            var body = QuotationEmailTemplate.Create(Document, Input.Message);
            await sender.SendQuotationAsync(Input.RecipientEmail.Trim(), Input.Subject.Trim(), body,
                userId.Value, revisionId, pdf, $"{Document.Number}-revision-{Document.Revision.RevisionNumber}.pdf",
                Input.SendId, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Email successfully sent to " + Input.RecipientEmail;
            ;
            return RedirectToPage("./Details", new { id, revisionId });
        }
        catch (InvalidOperationException exception)
        {
            if (exception is EmailSendException { CanRetry: true })
            {
                // Retain the failed attempt in history and use a fresh ID for the corrected submission.
                Input.SendId = Guid.NewGuid();
                ModelState.Remove("Input.SendId");
            }
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task<bool> LoadAsync(Guid id, Guid revisionId)
    {
        var document = await QuotationDocumentQueries.LoadAsync(db, id, revisionId);
        if (document is null || document.Revision.StatusId != (int)QuotationStatus.Approved
            || !document.Revision.ApprovalDate.HasValue) return false;
        var latestApproved = document.Revisions.FirstOrDefault(revision => revision.StatusId == (int)QuotationStatus.Approved);
        if (latestApproved?.Id != revisionId) return false;
        Document = document;
        return true;
    }
}
