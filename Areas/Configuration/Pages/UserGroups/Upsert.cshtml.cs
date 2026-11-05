using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BuildWise.Lib.Rights;
using BuildWise.Lib.Users;
using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.UserGroups
{
    public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
    {
        [BindProperty]
        public UserGroupUpsertViewModel Input { get; set; } = new();
        public UserGroup? UserGroup { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                return Page();
            }

            UserGroup = await db.UserGroups.AsNoTracking().FirstOrDefaultAsync(group => group.Id == id.Value);

            if (UserGroup is not null)
            {
                Input.Id = UserGroup.Id;
                Input.Name = UserGroup.Name;
                Input.RightsId = UserGroup.RightsId;
            }
            else
            {
               return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)

        {
            if (id != Input.Id)
            {
                return BadRequest();
            }

            if ((Input.RightsId & ~(int)AccessRights.All) != 0)
            {
                ModelState.AddModelError("Input.RightsId", "Select valid access rights.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please correct the errors and try again.");
                return Page();
            }

            var isNew = !id.HasValue;

            if (id.HasValue)
            {
                UserGroup = await db.UserGroups.FirstOrDefaultAsync(group => group.Id == id.Value);
                if (UserGroup is null)
                {
                    return NotFound();
                }

                UserGroup.Name = Input.Name;
                UserGroup.RightsId = Input.RightsId;
            }
            else
            {
                var creatorId = await currentUser.GetUserIdAsync();
               
                UserGroup = new UserGroup
                {
                    Id = Guid.NewGuid(),
                    Name = Input.Name,
                    RightsId = Input.RightsId,
                    CreatorId = creatorId.Value,
                    CreationDate = DateTime.Now
                    
                };

                if(await db.UserGroups.AnyAsync(x => x.Name == UserGroup.Name))
                {
                    ModelState.AddModelError("Input.Name", "A user group with this name already exists.");
                    return Page();

                }
                else
                {
                    db.UserGroups.Add(UserGroup);
                }
             
            }

            await db.SaveChangesAsync();

            TempData["SuccessMessage"] = isNew
                ? "User group created successfully."
                : "User group updated successfully.";

            return RedirectToPage("./Details", new { area = "Configuration", id = UserGroup?.Id });
        }
    }
}
