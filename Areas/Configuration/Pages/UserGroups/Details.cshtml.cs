using BuildWise.Data;
using BuildWise.Web.Services.GeneratedNumbers;
using BuildWise.Lib.Enums.Rights;
using BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Configuration.Pages.UserGroups;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public UserGroupDetailsViewModel GroupDetails { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var group = await db.UserGroups.AsNoTracking()
            .Include(group => group.Creator)
            .Include(group => group.Users)
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return NotFound();
        }

        await db.ApplyUserGroupNumberAsync(group);
        await db.ApplyUserNumbersAsync(group.Users);

        var creatorName = $"{group.Creator?.Name} {group.Creator?.Surname}".Trim();
        GroupDetails = new UserGroupDetailsViewModel
        {
            Id = group.Id,
            UserGroupNumber = group.UserGroupNumber,
            Name = group.Name,
            RightsId = group.RightsId,
            UserCount = group.Users.Count,
            CreatorId = group.CreatorId,
            CreatorName = string.IsNullOrWhiteSpace(creatorName)
                ? group.Creator?.Email ?? "Not available"
                : creatorName,
            CreationDate = group.CreationDate,
            AssignedRights = AccessRightsHelper.GetGroups()
                .SelectMany(rightsGroup => rightsGroup)
                .Where(right => (group.RightsId & (int)right.Value) == (int)right.Value)
                .ToList(),
            Members = group.Users.OrderBy(user => user.Name).ThenBy(user => user.Surname)
                .Select(user => new UserGroupMemberViewModel
                {
                    Id = user.Id,
                    UserNumber = user.UserNumber,
                    Name = $"{user.Name} {user.Surname}".Trim(),
                    Email = user.Email,
                    IsActive = user.IsActive
                })
                .ToList()
        };

        return Page();
    }
}
