using BuildWise.Lib.Enums;

namespace BuildWise.Web.Areas.CustomerPortal.ViewModels;

public sealed class CustomerProfileInputViewModel
{
    public ClientType ClientType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Address { get; set; } = string.Empty;
}

public sealed class CustomerPasswordInputViewModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
