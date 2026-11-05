namespace BuildWise.Web.Areas.Configuration.ViewModels.Currency;

public sealed record CurrencyIndexViewModel(
    Guid Id, string Name, string Code, string Symbol, string Creator, DateTime CreationDate)
{
    public string CurrencyNumber { get; init; } = string.Empty;
}
