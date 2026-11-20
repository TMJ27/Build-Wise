using Amazon;
using Amazon.Runtime.CredentialManagement;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;

namespace BuildWise.Web.Services.Email;

public class SesEmailSender(BuildWiseDbContext db, IConfiguration configuration,
    IHostEnvironment environment, ILogger<SesEmailSender> logger) : IEmailSender
{
    public Task<string> SendPasswordResetAsync(string recipient, string bodyHtml, Guid creatorId,
        CancellationToken cancellationToken = default)
        => SendCoreAsync(recipient, "Reset your BuildWise password", bodyHtml, creatorId,
            null, null, null, null, cancellationToken, redactBody: true);

    public Task<string> SendAsync(string recipient, string subject, string bodyHtml,
        Guid creatorId, CancellationToken cancellationToken = default)
        => SendCoreAsync(recipient, subject, bodyHtml, creatorId, null, null, null, null, cancellationToken);

    public Task<string> SendQuotationAsync(string recipient, string subject, string bodyHtml, Guid creatorId,
        Guid revisionId, byte[] pdf, string fileName, Guid sendId, CancellationToken cancellationToken = default)
        => SendCoreAsync(recipient, subject, bodyHtml, creatorId, revisionId, pdf, fileName, sendId, cancellationToken);

    private async Task<string> SendCoreAsync(string recipient, string subject, string bodyHtml,
        Guid creatorId, Guid? revisionId, byte[]? pdf, string? fileName, Guid? sendId, CancellationToken cancellationToken,
        bool redactBody = false)
    {
        if (sendId.HasValue && await db.EmailMessages.AnyAsync(message => message.Id == sendId, cancellationToken))
            throw new InvalidOperationException("This send has already been submitted. Check the email history before sending again.");
        if (revisionId.HasValue && !await db.QuotationRevisions.AnyAsync(revision => revision.Id == revisionId
            && revision.StatusId == (int)QuotationStatus.Approved && revision.ApprovalDate != null, cancellationToken))
            throw new InvalidOperationException("Only approved quotation revisions can be emailed.");
        var settings = await db.EmailConfigurations.AsNoTracking()
            .OrderBy(setting => setting.CreationDate).ThenBy(setting => setting.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsEnabled)
            throw new InvalidOperationException("Save and enable email settings before sending.");

        var attachments = new List<Amazon.SimpleEmailV2.Model.Attachment>();
        if (pdf is not null)
            attachments.Add(new Amazon.SimpleEmailV2.Model.Attachment
            {
                FileName = fileName, ContentType = "application/pdf",
                ContentDisposition = AttachmentContentDisposition.ATTACHMENT,
                ContentTransferEncoding = AttachmentContentTransferEncoding.BASE64,
                RawContent = new MemoryStream(pdf, writable: false)
            });
        var logo = revisionId.HasValue ? await LoadLogoAsync(revisionId.Value, cancellationToken) : null;
        if (logo is not null) attachments.Add(logo);
        bodyHtml = bodyHtml.Replace(QuotationEmailTemplate.LogoPlaceholder, logo is null ? string.Empty
            : "<img src=\"cid:company-logo\" alt=\"Company logo\" width=\"80\" style=\"display:block;width:80px;max-height:80px;object-fit:contain;margin-right:16px\">",
            StringComparison.Ordinal);

        var message = new EmailMessage
        {
            Id = sendId ?? Guid.NewGuid(), RecipientEmail = recipient, Subject = subject,
            BodyHtml = redactBody ? "Password reset instructions. The recovery link is omitted from email history." : bodyHtml,
            CreatorId = creatorId, CreationDate = DateTime.Now,
            StatusId = (int)EmailStatus.Sending, QuotationRevisionId = revisionId
        };
        db.EmailMessages.Add(message);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (sendId.HasValue)
        {
            db.Entry(message).State = EntityState.Detached;
            if (await db.EmailMessages.AnyAsync(existing => existing.Id == sendId, cancellationToken))
                throw new InvalidOperationException("This send has already been submitted. Check the email history before sending again.");
            throw;
        }

        var requestStarted = false;
        try
        {
            var clientConfig = new AmazonSimpleEmailServiceV2Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region),
                MaxErrorRetry = 0, Timeout = TimeSpan.FromSeconds(30)
            };
            using var client = CreateClient(clientConfig);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(30));
            requestStarted = true;
            var response = await client.SendEmailAsync(new SendEmailRequest
            {
                FromEmailAddress = new MailAddress(settings.SenderEmail, settings.SenderName).ToString(),
                Destination = new Destination { ToAddresses = [recipient] },
                ReplyToAddresses = string.IsNullOrWhiteSpace(settings.ReplyToEmail)
                    ? [] : [settings.ReplyToEmail],
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content { Data = subject, Charset = "UTF-8" },
                        Body = new Body { Html = new Content { Data = bodyHtml, Charset = "UTF-8" } },
                        Attachments = attachments
                    }
                }
            }, deadline.Token);
            message.ProviderMessageId = response.MessageId;
            message.StatusId = (int)EmailStatus.Sent;
            message.SentDate = DateTime.Now;
        }
        catch (Exception exception)
        {
            var canRetry = !requestStarted || exception is AmazonSimpleEmailServiceV2Exception rejection
                && (((int)rejection.StatusCode >= 400 && (int)rejection.StatusCode < 500 && (int)rejection.StatusCode != 408)
                    || rejection.ErrorCode is "AccessDenied" or "AccessDeniedException" or "MessageRejected" or "BadRequestException");
            // Avoid persisting credentials or provider response bodies in email history.
            message.StatusId = (int)EmailStatus.Failed;
            message.FailureReason = exception is AmazonSimpleEmailServiceV2Exception aws
                ? $"Amazon SES rejected the request ({aws.ErrorCode}). Check permissions, region and verified addresses."
                : exception is OperationCanceledException
                    ? "The sending request timed out or was cancelled. Delivery is unconfirmed; check before sending again."
                    : "Email could not be sent. Check the AWS profile and network connection.";
            logger.LogWarning("Email {EmailId} failed with {ExceptionType}", message.Id, exception.GetType().Name);
            await db.SaveChangesAsync(CancellationToken.None);
            if (environment.IsDevelopment() && exception is AmazonSimpleEmailServiceV2Exception denied
                && denied.ErrorCode is "AccessDenied" or "AccessDeniedException")
            {
                // AWS's authorization message identifies the denied action/resource.
                // Show it only during development; do not store the provider response.
                throw new EmailSendException(
                    $"Amazon SES permission denied: {denied.Message} (Request ID: {denied.RequestId})", canRetry);
            }
            throw new EmailSendException(message.FailureReason, canRetry);
        }

        // Keep persistence errors separate from SES errors: SES may already have accepted this message.
        await db.SaveChangesAsync(CancellationToken.None);
        return message.ProviderMessageId!;
    }

    private async Task<Amazon.SimpleEmailV2.Model.Attachment?> LoadLogoAsync(Guid revisionId, CancellationToken cancellationToken)
    {
        var key = await db.QuotationRevisions.Where(revision => revision.Id == revisionId)
            .Select(revision => revision.CompanyLogoStorageKeySnapshot).SingleAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(key))
            key = await db.Companies.OrderBy(company => company.CreationDate)
                .Select(company => company.LogoStorageKey).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("/logos/", StringComparison.Ordinal)
            || key[7..] != Path.GetFileName(key)) return null;
        var root = environment is IWebHostEnvironment web ? web.WebRootPath : Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(root, "logos", Path.GetFileName(key));
        if (!File.Exists(path) || new FileInfo(path).Length > 2 * 1024 * 1024) return null;
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var png = bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var jpeg = bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 });
        if (!png && !jpeg) return null;
        return new Amazon.SimpleEmailV2.Model.Attachment
        {
            FileName = png ? "company-logo.png" : "company-logo.jpg", ContentId = "company-logo",
            ContentType = png ? "image/png" : "image/jpeg", ContentDisposition = AttachmentContentDisposition.INLINE,
            ContentTransferEncoding = AttachmentContentTransferEncoding.BASE64, RawContent = new MemoryStream(bytes, writable: false)
        };
    }

    protected virtual IAmazonSimpleEmailServiceV2 CreateClient(AmazonSimpleEmailServiceV2Config clientConfig)
    {
        if (environment.IsDevelopment())
        {
            var profile = configuration["Email:AwsProfile"];
            if (string.IsNullOrWhiteSpace(profile) ||
                !new CredentialProfileStoreChain().TryGetAWSCredentials(profile, out var credentials))
                throw new InvalidOperationException("The local AWS email profile is not configured.");
            return new AmazonSimpleEmailServiceV2Client(credentials, clientConfig);
        }

        return new AmazonSimpleEmailServiceV2Client(clientConfig);
    }
}
