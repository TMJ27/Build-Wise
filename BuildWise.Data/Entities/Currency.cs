namespace BuildWise.Data;

public partial class Currency
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public DateTime CreationDate { get; set; }
    public virtual User Creator { get; set; } = null!;
}
