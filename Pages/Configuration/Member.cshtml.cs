using System.ComponentModel.DataAnnotations;
using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Configuration;

public class MemberModel(AccessWorkspace access) : PageModel
{
    [BindProperty]
    public MemberInput Input { get; set; } = new();

    public AccessMember? Existing { get; private set; }

    public IActionResult OnGet(Guid? id)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (Existing is not null)
        {
            Input = new MemberInput
            {
                Name = Existing.Name,
                Email = Existing.Email,
                Role = Existing.Role,
                IsActive = Existing.IsActive,
                Version = Existing.Version
            };
        }

        return Page();
    }

    public IActionResult OnPost(Guid? id)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (!AccessWorkspace.EditableRoles.Contains(Input.Role))
        {
            ModelState.AddModelError("Input.Role", "Choose a valid team role.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var saved = access.SaveMember(id, Input.Name, Input.Email, Input.Role, Input.IsActive, Input.Version);

        if (!saved.Id.HasValue)
        {
            ModelState.AddModelError(string.Empty, saved.Error!);
            return Page();
        }

        TempData["AccessMessage"] = Existing is null ? "Team member added. No invitation email has been sent." : "Team member settings updated.";
        return RedirectToPage("Access");
    }

    private IActionResult? Load(Guid? id)
    {
        Existing = id.HasValue ? access.FindMember(id.Value) : null;

        if (id.HasValue && Existing is null)
        {
            return NotFound();
        }

        return Existing?.Role == "Owner" ? StatusCode(403) : null;
    }

    public sealed class MemberInput
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
}
