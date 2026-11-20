using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public sealed class LineInputViewModel
{
    [Required, StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "100000")]
    public decimal Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0", "1000000")]
    public decimal Rate { get; set; }
}
