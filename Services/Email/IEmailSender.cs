namespace BuildWise.Web.Services.Email;

public interface IEmailSender
{
    Task<string> SendPasswordResetAsync(string recipient, string bodyHtml, Guid creatorId,
        CancellationToken cancellationToken = default)
        => SendAsync(recipient, "Reset your BuildWise password", bodyHtml, creatorId, cancellationToken);

    Task<string> SendAsync(string recipient, string subject, string bodyHtml, Guid creatorId,
        CancellationToken cancellationToken = default);

    Task<string> SendQuotationAsync(string recipient, string subject, string bodyHtml, Guid creatorId,
        Guid revisionId, byte[] pdf, string fileName, Guid sendId, CancellationToken cancellationToken = default);
}
