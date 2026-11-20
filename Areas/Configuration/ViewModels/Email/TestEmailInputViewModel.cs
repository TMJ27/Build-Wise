using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Email;

public class TestEmailInputViewModel
{
    [Required, EmailAddress, StringLength(254)]
    public string RecipientEmail { get; set; } = string.Empty;
}
