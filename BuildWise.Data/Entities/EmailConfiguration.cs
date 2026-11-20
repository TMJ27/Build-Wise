namespace BuildWise.Data;

public partial class EmailConfiguration
{
    public Guid Id { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;

    public string? ReplyToEmail { get; set; }

    public string Region { get; set; } = "eu-north-1";

    public bool IsEnabled { get; set; }

    public Guid CreatorId { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdate { get; set; }

    public virtual User Creator { get; set; } = null!;
}
