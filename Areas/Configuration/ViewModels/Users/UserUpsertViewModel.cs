using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Users;

public sealed class UserUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Surname { get; set; }

    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    public Guid? GroupId { get; set; }

    public bool IsActive { get; set; } = true;

}
