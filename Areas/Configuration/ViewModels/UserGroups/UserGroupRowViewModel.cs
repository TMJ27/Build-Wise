namespace BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;

public sealed class UserGroupRowViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RightsId { get; set; }

    public int UserCount { get; set; }

    public string CreatorName { get; set; } = string.Empty;

    public DateTime CreationDate { get; set; }
}
