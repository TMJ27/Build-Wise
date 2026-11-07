using BuildWise.Lib.Enums.Rights;

namespace BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;

public sealed class UserGroupDetailsViewModel
{
    public Guid Id { get; set; }
    public string UserGroupNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int RightsId { get; set; }

    public int UserCount { get; set; }

    public Guid CreatorId { get; set; }

    public string CreatorName { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; }

    public List<AccessRightOption> AssignedRights { get; set; } = [];

    public List<UserGroupMemberViewModel> Members { get; set; } = [];
}
