using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class ForgotPasswordInputViewModel
{
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordInputViewModel
{
    [Required, StringLength(4096)]
    public string Token { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password), ErrorMessage = "The passwords do not match."), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
