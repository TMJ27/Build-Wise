using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class CustomerLoginViewModel
{
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
