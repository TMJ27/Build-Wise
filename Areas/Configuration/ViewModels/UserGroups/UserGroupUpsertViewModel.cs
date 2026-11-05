using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;

public sealed class UserGroupUpsertViewModel
{
    public Guid? Id { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Group name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Access rights")]
    public int RightsId { get; set; }
}
