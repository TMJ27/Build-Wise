using System.Security.Claims;
using System.Text.Json;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.Pages;
using BuildWise.Web.Services.Email;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class QuotationSendChecks
{
    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "staff@example.test", PasswordHash = "test" };
        var client = new Client { Id = Guid.NewGuid(), Name = "Customer", Email = "customer@example.test", Address = "Site", Creator = user };
        var request = new ClientRequest { Id = Guid.NewGuid(), Title = "Work", SiteAddress = "Site", ReferenceNumber = "REQ-20261112-0001", Client = client, Creator = user };
        var quotation = new Quotation { Id = Guid.NewGuid(), QuotationNumber = "legacy", Request = request, Creator = user };
        var revision = new QuotationRevision
        {
            Id = Guid.NewGuid(), Quotation = quotation, Creator = user, RevisionNumber = 1,
            StatusId = (int)QuotationStatus.Approved, ApprovalDate = DateTime.Now,
            Title = "Reviewed electrical installation", CompanyNameSnapshot = "Saved company",
            CompanyEmailSnapshot = "company@example.test", CompanyAddressSnapshot = "Harare, Zimbabwe",
            CustomerNameSnapshot = "Saved customer", CustomerEmailSnapshot = "customer@example.test",
            SiteAddressSnapshot = "Saved project site", RequestDescriptionSnapshot = "Installation",
            CreationDate = new DateTime(2026, 11, 12), IssueDate = new DateTime(2026, 11, 12), ValidUntil = new DateTime(2026, 11, 26),
            IncludesPrices = true, CurrencyCode = "USD", Subtotal = 4000, TaxAmount = 600, Total = 4600,
            Terms = "Payment arrangements must be confirmed before work starts.", Exclusions = "Additional work is quoted separately."
        };
        var items = Enumerable.Range(1, 40).Select(index => new
        {
            Title = $"Service {index:D2}", Description = "Install and test the specified equipment. Confirm access and measurements before installation.",
            Quantity = 2, UnitCodeSnapshot = "m²", LineAmount = 100m
        }).ToList();
        revision.ApprovedDocumentJson = JsonSerializer.Serialize(new
        {
            revision.RevisionNumber, revision.Title, revision.CompanyNameSnapshot, revision.CompanyEmailSnapshot,
            revision.CompanyAddressSnapshot, revision.CustomerNameSnapshot, revision.CustomerEmailSnapshot,
            revision.SiteAddressSnapshot, revision.IssueDate, revision.ValidUntil, revision.IncludesPrices,
            revision.CurrencyCode, revision.Subtotal, revision.TaxAmount, revision.Total, revision.Terms, revision.Exclusions,
            Items = items
        });
        db.AddRange(user, client, request, quotation, revision);
        await db.SaveChangesAsync();
        var fake = new CapturingSender();
        SendModel Form() => new(db, new CurrentUser(user.Id), fake)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        var form = Form();
        Check(await form.OnGetAsync(quotation.Id, revision.Id) is PageResult
            && form.Input.RecipientEmail == "customer@example.test", "Send must load the approved customer's email.");
        form.Input.Message = "Please review <script>alert('x')</script>";
        var pdf = "%PDF-1.7\n"u8.ToArray().Concat(new byte[1024]).ToArray();
        form.PdfAttachment = new FormFile(new MemoryStream(pdf), 0, pdf.Length, "PdfAttachment", "quotation.pdf");
        Check(await form.OnPostAsync(quotation.Id, revision.Id) is RedirectToPageResult,
            "A confirmed approved quotation must send and return to Details.");
        Check(fake.RevisionId == revision.Id && fake.Body!.Contains("&lt;script&gt;")
            && fake.Pdf is { Length: > 1000 } && fake.FileName!.EndsWith("-revision-1.pdf"),
            "Send must encode message text and attach the correct revision PDF.");
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
        var pdfData = JsonSerializer.Serialize(QuotationPdfData.Create(form.Document));
        using var frozen = JsonDocument.Parse(pdfData);
        Check(frozen.RootElement.GetProperty("Items").GetArrayLength() == 40,
            "Browser PDF data must contain the frozen approved items.");
        await File.WriteAllTextAsync(Path.Combine(root, "obj/jspdf-quotation-data.json"), pdfData);
        var stale = Form();
        await stale.OnGetAsync(quotation.Id, revision.Id);
        stale.Input.Version = "stale";
        Check(await stale.OnPostAsync(quotation.Id, revision.Id) is PageResult && !stale.ModelState.IsValid,
            "Stale send forms must be blocked.");
        Check(await Form().OnGetAsync(quotation.Id, Guid.NewGuid()) is NotFoundResult,
            "Foreign revisions must not be sent.");
        var rejected = Form();
        await rejected.OnGetAsync(quotation.Id, revision.Id);
        rejected.PdfAttachment = new FormFile(new MemoryStream(pdf), 0, pdf.Length, "PdfAttachment", "quotation.pdf");
        var rejectedId = rejected.Input.SendId;
        rejected.ModelState.SetModelValue("Input.SendId", rejectedId.ToString(), rejectedId.ToString());
        rejected.ModelState.MarkFieldValid("Input.SendId");
        fake.Error = new EmailSendException("SES rejected the recipient.", true);
        Check(await rejected.OnPostAsync(quotation.Id, revision.Id) is PageResult
            && rejected.Input.SendId != rejectedId && !rejected.ModelState.ContainsKey("Input.SendId"),
            "Definite failures must issue a fresh submission ID, including the rendered hidden field.");
        fake.Error = null;
        var corrected = Form();
        corrected.Input = rejected.Input;
        corrected.Input.RecipientEmail = "verified@example.test";
        corrected.PdfAttachment = rejected.PdfAttachment;
        Check(await corrected.OnPostAsync(quotation.Id, revision.Id) is RedirectToPageResult,
            "A corrected recipient must be sendable after rejection.");
        var uncertain = Form();
        await uncertain.OnGetAsync(quotation.Id, revision.Id);
        uncertain.PdfAttachment = rejected.PdfAttachment;
        var uncertainId = uncertain.Input.SendId;
        fake.Error = new EmailSendException("Delivery is unconfirmed.", false);
        await uncertain.OnPostAsync(quotation.Id, revision.Id);
        Check(uncertain.Input.SendId == uncertainId, "An uncertain outcome must retain duplicate protection.");
        fake.Error = null;
        revision.StatusId = (int)QuotationStatus.Draft;
        await db.SaveChangesAsync();
        Check(await Form().OnGetAsync(quotation.Id, revision.Id) is NotFoundResult, "Drafts must not be sent.");
        Console.WriteLine("PASS: approved-only send, recipient, encoded message, uploaded PDF, frozen PDF data, stale and foreign revision guards. No live emails sent.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class CapturingSender : IEmailSender
    {
        public Guid RevisionId;
        public string? Body;
        public byte[]? Pdf;
        public string? FileName;
        public Exception? Error;
        public Task<string> SendAsync(string recipient, string subject, string bodyHtml, Guid creatorId, CancellationToken token = default) => throw new NotSupportedException();
        public Task<string> SendQuotationAsync(string recipient, string subject, string bodyHtml, Guid creatorId, Guid revisionId,
            byte[] pdf, string fileName, Guid sendId, CancellationToken token = default)
        {
            if (Error is not null) throw Error;
            RevisionId = revisionId; Body = bodyHtml; Pdf = pdf; FileName = fileName;
            return Task.FromResult("provider-id");
        }
    }
    private sealed class CurrentUser(Guid id) : ICurrentUserService
    {
        public Task<Guid?> GetUserIdAsync() => Task.FromResult<Guid?>(id);
        public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    }
    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
