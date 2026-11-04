namespace BuildWise.Web.Areas.Configuration.ViewModels.Currency;

public sealed record CurrencyRowViewModel(
    Guid Id, string Name, string Code, string Symbol, string Creator, DateTime CreationDate);
