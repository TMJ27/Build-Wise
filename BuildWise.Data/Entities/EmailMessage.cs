namespace BuildWise.Data;

public partial class EmailMessage
{
    public Guid Id { get; set; }

    public string RecipientEmail { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string BodyHtml { get; set; } = string.Empty;

    public int StatusId { get; set; } = (int)BuildWise.Lib.Enums.Statuses.EmailStatus.Pending;

    public Guid? QuotationRevisionId { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? FailureReason { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? SentDate { get; set; }

    public virtual User Creator { get; set; } = null!;

    public virtual QuotationRevision? QuotationRevision { get; set; }
}
