namespace BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;

public sealed class UserGroupMemberViewModel
{
    public Guid Id { get; set; }
    public string UserNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
