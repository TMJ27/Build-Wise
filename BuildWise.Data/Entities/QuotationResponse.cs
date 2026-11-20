namespace BuildWise.Data;

public class QuotationResponse
{
    public Guid Id { get; set; }
    public Guid QuotationRevisionId { get; set; }
    public Guid CustomerAccountId { get; set; }
    public int StatusId { get; set; }
    public string? Reason { get; set; }
    public string CustomerNameSnapshot { get; set; } = string.Empty;
    public string CustomerEmailSnapshot { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public virtual QuotationRevision QuotationRevision { get; set; } = null!;
    public virtual CustomerAccount CustomerAccount { get; set; } = null!;
}
