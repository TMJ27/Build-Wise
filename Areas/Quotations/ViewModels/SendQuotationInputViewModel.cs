using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public class SendQuotationInputViewModel
{
    [Required, EmailAddress, StringLength(254)]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required, StringLength(200), RegularExpression(@"[^\r\n]+", ErrorMessage = "Enter a subject on one line.")]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(5000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Version { get; set; } = string.Empty;

    public Guid SendId { get; set; }
}
