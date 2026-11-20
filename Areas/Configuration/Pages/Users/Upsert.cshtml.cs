using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.Areas.Configuration.ViewModels.Users;
using BuildWise.Lib.Users;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace BuildWise.Web.Areas.Configuration.Pages.Users
{
    public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
    {
        public User? Users { get; set; } = new();
        [BindProperty]
        public UserUpsertViewModel Input { get; set; } = new();
        public List<SelectListItem> Groups { get; private set; } = [];

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id.HasValue)
            {
                Users = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (Users is null) return NotFound();
                Input = new UserUpsertViewModel
                {
                    Id = Users.Id, Name = Users.Name ?? string.Empty, Surname = Users.Surname,
                    Email = Users.Email, Phone = Users.Phone, GroupId = Users.GroupId, IsActive = Users.IsActive
                };
            }
            await LoadGroupsAsync();
            return Page();

        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id != Input.Id) return BadRequest();
            var creatorId = await currentUser.GetUserIdAsync();
            if (!creatorId.HasValue) return Unauthorized();
            if (Input.GroupId.HasValue && !await db.UserGroups.AnyAsync(group => group.Id == Input.GroupId))
                ModelState.AddModelError("Input.GroupId", "Select a configured user group.");
            Input.Email = (Input.Email ?? string.Empty).Trim();
            if (await db.Users.AnyAsync(user => user.Id != Input.Id && user.Email.ToUpper() == Input.Email.ToUpper()))
                ModelState.AddModelError("Input.Email", "A user with this email already exists.");
            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync();
                return Page();
            }

            Users = await db.Users.FirstOrDefaultAsync(x => x.Id == Input.Id);

            if (id.HasValue && Users is null) return NotFound();

            if (Users is not null)
            {
                Users.Name = Input.Name;
                Users.Surname = Input.Surname;
                Users.Email = Input.Email;
                Users.Phone = Input.Phone;
                Users.GroupId = Input.GroupId;
                Users.IsActive = Input.IsActive;
                Users.LastUpdated = DateTime.Now;

                db.Users.Update(Users);

                TempData["SuccessMessage"] = "User updated successfully.";
            }
            else
            {
                var NewUser = new User
                {
                    Id = Guid.NewGuid(),
                    Name = Input.Name,
                    Surname = Input.Surname,
                    Email = Input.Email,
                    Phone = Input.Phone,
                    GroupId = Input.GroupId,
                    IsActive = Input.IsActive,
                    CreationDate = DateTime.Now,
                    CreatorId = creatorId.Value,
                    SecurityStamp = Guid.NewGuid().ToString("N"),
                };
                // Staff authenticate with Cognito; do not create a usable local password.
                NewUser.PasswordHash = new PasswordHasher<User>().HashPassword(NewUser,
                    Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));

                db.Users.Add(NewUser);
                Users = NewUser;

                TempData["SuccessMessage"] = "User created successfully.";

            }

            await db.SaveChangesAsync();



            return RedirectToPage("/Users/Details", new { id = Users?.Id ?? Input.Id });
        }
        private async Task LoadGroupsAsync() => Groups = await db.UserGroups.AsNoTracking()
            .OrderBy(group => group.Name).Select(group => new SelectListItem
            { Value = group.Id.ToString(), Text = group.Name }).ToListAsync();
      
    }
}
