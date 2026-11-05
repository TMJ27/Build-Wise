using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.Areas.Configuration.ViewModels.Users;
using BuildWise.Web.Services;

namespace BuildWise.Web.Areas.Configuration.Pages.Users
{
    public class UpsertModel(BuildWiseDbContext db, CurrentUserService currentUser) : PageModel
    {
        public User? Users { get; set; } = new();
        [BindProperty]
        public UserUpsertViewModel Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please correct the errors and try again.");
                return Page();
            }

            Users = await db.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (Users is not null)
            {
                Users.Name = Input.Name;
                Users.Surname = Input.Surname;
                Users.Email = Input.Email;
                Users.Phone = Input.Phone;
                Users.GroupId = Input.GroupId;
                Users.IsActive = Input.IsActive;
            }

            else
            {
              return NotFound();
            }
         
            return Page();

        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Users = await db.Users.FirstOrDefaultAsync(x => x.Id == Input.Id);

            var CurrentUserId = await currentUser.GetUserIdAsync();

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
                if(await db.Users.AnyAsync(x => x.Email == Input.Email))
                {
                    ModelState.AddModelError("Input.Email", "A user with this email already exists.");
                    return Page();
                }
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
                    CreatorId = (Guid)CurrentUserId,
                };

                db.Users.Add(NewUser);

                TempData["SuccessMessage"] = "User created successfully.";

            }

            await db.SaveChangesAsync();



            return RedirectToPage("/Users/Index");
        }
      
    }
}
