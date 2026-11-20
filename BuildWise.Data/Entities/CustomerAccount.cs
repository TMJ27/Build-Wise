namespace BuildWise.Data;

public sealed class CustomerAccount
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreationDate { get; set; }
    public Client Client { get; set; } = null!;
    public User User { get; set; } = null!;
}
