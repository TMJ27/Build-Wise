using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Areas.Configuration.ViewModels.Email;

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var userId = Guid.NewGuid();
db.Users.Add(new User { Id = userId, Email = "staff@example.test", PasswordHash = "test" });
var settings = new EmailConfiguration
{
    Id = Guid.NewGuid(), SenderName = "BuildWise", SenderEmail = "sender@example.test",
    ReplyToEmail = "reply@example.test", Region = "eu-north-1", CreatorId = userId
};
db.EmailConfigurations.Add(settings);
await db.SaveChangesAsync();
var client = new StubClient();
var sender = new TestSender(db, client);
try { await sender.SendAsync("recipient@example.test", "Test", "<p>Test</p>", userId); throw new Exception("Disabled sending allowed."); }
catch (InvalidOperationException) { }
Check(!await db.EmailMessages.AnyAsync(), "Disabled sends must not create sending records.");
settings.IsEnabled = true;
await db.SaveChangesAsync();
await sender.SendAsync("recipient@example.test", "Test", "<p>Test</p>", userId);
var sent = await db.EmailMessages.SingleAsync();
Check(sent.StatusId == (int)EmailStatus.Sent && sent.ProviderMessageId == "provider-id" && sent.SentDate.HasValue,
    "Accepted sends must persist provider ID and sent time.");
Check(client.Request!.FromEmailAddress.Contains(settings.SenderEmail)
    && client.Request.Destination.ToAddresses.Single() == "recipient@example.test"
    && client.Request.ReplyToAddresses.Single() == settings.ReplyToEmail
    && client.Request.Content.Simple.Body.Html.Data == "<p>Test</p>", "Request must use saved settings and supplied content.");
client.Fail = true;
try { await sender.SendAsync("recipient@example.test", "Failure", "<p>Test</p>", userId); throw new Exception("Failure not reported."); }
catch (InvalidOperationException) { }
var failed = await db.EmailMessages.SingleAsync(message => message.Subject == "Failure");
Check(failed.StatusId == (int)EmailStatus.Failed && !failed.SentDate.HasValue
    && failed.FailureReason!.Contains("MessageRejected") && !failed.FailureReason.Contains("private provider detail"),
    "Rejected sends must persist safe failure information.");
var invalidSettings = new EmailSettingsInputViewModel { SenderEmail = "invalid" };
Check(!Validator.TryValidateObject(invalidSettings,
    new ValidationContext(invalidSettings), new List<ValidationResult>(), true), "Invalid sender must fail validation.");
Console.WriteLine("PASS: disabled sending, SES request, accepted/rejected email history and input validation. No live emails sent.");

var upsert = new BuildWise.Web.Areas.Configuration.Pages.Email.UpsertModel(db, null!);
await upsert.OnGetAsync();
Check(upsert.IsConfigured && upsert.Input.SenderEmail == settings.SenderEmail,
    "Upsert must load the saved configuration independently.");
var details = new BuildWise.Web.Areas.Configuration.Pages.Email.DetailsModel(db, null!, sender);
Check(await details.OnGetAsync() is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
    && details.Settings.SenderEmail == settings.SenderEmail && details.RecentMessages.Count == 2,
    "Details must load saved settings and email history.");
await using var emptyDb = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
Check(await new BuildWise.Web.Areas.Configuration.Pages.Email.DetailsModel(emptyDb, null!, sender)
    .OnGetAsync() is Microsoft.AspNetCore.Mvc.RedirectToPageResult { PageName: "./Upsert" },
    "Details must open Upsert for first-time setup.");
Console.WriteLine("PASS: separate Details/Upsert loading, email history and first-time setup.");

var quotationRevision = new QuotationRevision
{
    Id = Guid.NewGuid(), QuotationId = Guid.NewGuid(), CreatorId = userId,
    Title = "Reviewed work", CompanyNameSnapshot = "Company", CustomerNameSnapshot = "Customer",
    SiteAddressSnapshot = "Site", RequestDescriptionSnapshot = "Work", StatusId = (int)BuildWise.Lib.Enums.Statuses.QuotationStatus.Draft
};
db.QuotationRevisions.Add(quotationRevision);
await db.SaveChangesAsync();
var sendId = Guid.NewGuid();
try
{
    await sender.SendQuotationAsync("recipient@example.test", "Quote", "<p>Quote</p>", userId,
        quotationRevision.Id, [1, 2, 3], "quotation.pdf", sendId);
    throw new Exception("Draft quotation was sent.");
}
catch (InvalidOperationException) { }
quotationRevision.StatusId = (int)BuildWise.Lib.Enums.Statuses.QuotationStatus.Approved;
quotationRevision.ApprovalDate = DateTime.Now;
await db.SaveChangesAsync();
client.Fail = false;
await sender.SendQuotationAsync("recipient@example.test", "Quote", "<p>Quote</p>", userId,
    quotationRevision.Id, [1, 2, 3], "quotation.pdf", sendId);
Check(client.Request!.Content.Simple.Attachments.Single().FileName == "quotation.pdf"
    && client.Request.Content.Simple.Attachments.Single().RawContent.ToArray().SequenceEqual(new byte[] { 1, 2, 3 })
    && (await db.EmailMessages.SingleAsync(message => message.Id == sendId)).QuotationRevisionId == quotationRevision.Id,
    "SES must receive the PDF attachment and history must link to the approved revision.");
try
{
    await sender.SendQuotationAsync("recipient@example.test", "Quote", "<p>Quote</p>", userId,
        quotationRevision.Id, [1, 2, 3], "quotation.pdf", sendId);
    throw new Exception("Repeated send token was accepted.");
}
catch (InvalidOperationException) { }
Check(await db.EmailMessages.CountAsync(message => message.Id == sendId) == 1, "Repeated submission must not duplicate history.");
Console.WriteLine("PASS: draft rejection, PDF attachment, revision audit link and duplicate submission protection.");

var logoRoot = Path.Combine(Path.GetTempPath(), "buildwise-logo-check-" + Guid.NewGuid());
Directory.CreateDirectory(Path.Combine(logoRoot, "wwwroot", "logos"));
try
{
    var logoBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a/2kAAAAASUVORK5CYII=");
    await File.WriteAllBytesAsync(Path.Combine(logoRoot, "wwwroot", "logos", "test.png"), logoBytes);
    quotationRevision.CompanyLogoStorageKeySnapshot = "/logos/test.png";
    await db.SaveChangesAsync();
    var branded = QuotationEmailTemplate.Create(new BuildWise.Web.Areas.Quotations.ViewModels.QuotationDocumentViewModel
    {
        Number = "QT-20261112-0001", Revision = quotationRevision
    }, "Please review <script>unsafe</script>");
    Check(branded.Contains("QT-20261112-0001") && branded.Contains("&lt;script&gt;") && !branded.Contains("<script>"),
        "Branded email must identify the quotation and encode message content.");
    await new TestSender(db, client, logoRoot).SendQuotationAsync("recipient@example.test", "Branded quote", branded,
        userId, quotationRevision.Id, [1, 2, 3], "quotation.pdf", Guid.NewGuid());
    var inline = client.Request!.Content.Simple.Attachments.Single(item => item.ContentId == "company-logo");
    Check(inline.ContentDisposition == AttachmentContentDisposition.INLINE && inline.RawContent.ToArray().SequenceEqual(logoBytes)
        && client.Request.Content.Simple.Body.Html.Data.Contains("cid:company-logo")
        && !client.Request.Content.Simple.Body.Html.Data.Contains(QuotationEmailTemplate.LogoPlaceholder),
        "Logo must be embedded inline, without requiring access to the development website.");
    Console.WriteLine("PASS: branded HTML, escaped content and inline company logo.");
}
finally { Directory.Delete(logoRoot, recursive: true); }

client.Fail = true;
try { await sender.SendAsync("bad@example.test", "Rejected retry check", "test", userId); }
catch (EmailSendException exception) { Check(exception.CanRetry, "SES rejection must permit a corrected attempt."); }
client.Fail = false;
client.Timeout = true;
try { await sender.SendAsync("recipient@example.test", "Timeout retry check", "test", userId); }
catch (EmailSendException exception) { Check(!exception.CanRetry, "A timeout must not permit an automatic retry."); }
Console.WriteLine("PASS: definite rejection permits correction; uncertain delivery retains duplicate protection.");

client.Timeout = false;
await sender.SendPasswordResetAsync("recipient@example.test", "<a href=\"https://example.test/reset?token=secret\">Reset</a>", userId);
Check(client.Request!.Content.Simple.Body.Html.Data.Contains("token=secret")
    && !(await db.EmailMessages.OrderByDescending(message => message.CreationDate).FirstAsync()).BodyHtml.Contains("token=secret"),
    "Recovery email must reach SES while its reset link is excluded from stored email history.");
Console.WriteLine("PASS: recovery link redaction in email history.");

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

sealed class TestSender(BuildWiseDbContext db, StubClient client, string root = ".") : SesEmailSender(db,
    new ConfigurationBuilder().Build(), new TestEnvironment { ContentRootPath = root }, NullLogger<SesEmailSender>.Instance)
{
    protected override IAmazonSimpleEmailServiceV2 CreateClient(AmazonSimpleEmailServiceV2Config config) => client;
}
sealed class StubClient() : AmazonSimpleEmailServiceV2Client(new AnonymousAWSCredentials(), Amazon.RegionEndpoint.EUNorth1)
{
    public SendEmailRequest? Request { get; private set; }
    public bool Fail { get; set; }
    public bool Timeout { get; set; }
    public override Task<SendEmailResponse> SendEmailAsync(SendEmailRequest request, CancellationToken token = default)
    {
        Request = request;
        if (Timeout) throw new OperationCanceledException();
        if (Fail) throw new AmazonSimpleEmailServiceV2Exception("private provider detail") { ErrorCode = "MessageRejected" };
        return Task.FromResult(new SendEmailResponse { MessageId = "provider-id" });
    }
}
sealed class TestEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "EmailChecks";
    public string ContentRootPath { get; set; } = ".";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
