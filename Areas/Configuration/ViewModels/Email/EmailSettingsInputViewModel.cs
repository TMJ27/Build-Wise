using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Email;

public class EmailSettingsInputViewModel
{
    [Required, StringLength(200), RegularExpression(@"[^\r\n]+", ErrorMessage = "Enter a sender name on one line.")]
    public string SenderName { get; set; } = "BuildWise";

    [Required, EmailAddress, StringLength(254)]
    public string SenderEmail { get; set; } = "tavomadanhire@gmail.com";

    [EmailAddress, StringLength(254)]
    public string? ReplyToEmail { get; set; }

    [Required, StringLength(50), RegularExpression(@"[a-z]{2}(-[a-z]+)+-\d+", ErrorMessage = "Enter an AWS region such as eu-north-1.")]
    public string Region { get; set; } = "eu-north-1";

    public bool IsEnabled { get; set; }
}
