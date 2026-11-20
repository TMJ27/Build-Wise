using System.ComponentModel.DataAnnotations;
using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class CustomerRegistrationViewModel
{
    [EnumDataType(typeof(ClientType))]
    public ClientType ClientType { get; set; }
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [Required, StringLength(50)]
    public string Address { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 12)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128), Compare(nameof(Password), ErrorMessage = "The passwords must match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
