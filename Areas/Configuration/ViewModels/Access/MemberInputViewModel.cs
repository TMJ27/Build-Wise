using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Access;

public sealed class MemberInputViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Estimator";

    public bool IsActive { get; set; } = true;
    public long Version { get; set; }
}
