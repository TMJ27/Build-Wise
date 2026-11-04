using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Currency;

public sealed class CurrencyInputViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression("[A-Za-z]{3}", ErrorMessage = "Enter a three-letter currency code.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Symbol { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select the creator.")]
    public Guid? CreatorId { get; set; }
}
