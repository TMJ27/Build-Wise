namespace BuildWise.Web.Areas.Configuration.ViewModels.Currency;

public class CurrencyDetailsViewModel
{
    public Guid Id { get; set; }
    public string CurrencyNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}
