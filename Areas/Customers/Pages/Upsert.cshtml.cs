using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Customers.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Customers.Pages;

public class UpsertModel(BuildWiseDbContext db, ICurrentUserService currentUser) : PageModel
{
    [BindProperty]
    public CustomerUpsertViewModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return Page();
        }

        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(client => client.Id == id.Value);
        if (client is null)
        {
            return NotFound();
        }

        Input = new CustomerUpsertViewModel
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            Address = client.Address,
            ClientTypeId = (ClientType)client.ClientTypeId,
            IsActive = client.IsActive
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
    {
        if (id != Input.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Client client;
        if (id.HasValue)
        {
            var existing = await db.Clients.FirstOrDefaultAsync(client => client.Id == id.Value);
            if (existing is null)
            {
                return NotFound();
            }
            client = existing;
        }
        else
        {
            var creatorId = await currentUser.GetUserIdAsync();
            if (!creatorId.HasValue)
            {
                return Unauthorized();
            }

            client = new Client
            {
                Id = Guid.NewGuid(),
                CreationDate = DateTime.Now,
                CreatorId = creatorId.Value
            };
            db.Clients.Add(client);
        }

        client.Name = Input.Name.Trim();
        client.Email = Input.Email.Trim();
        client.Phone = Input.Phone?.Trim();
        client.Address = Input.Address.Trim();
        client.ClientTypeId = (int)Input.ClientTypeId!.Value;
        client.IsActive = Input.IsActive;

        await db.SaveChangesAsync();
        TempData["SuccessMessage"] = id.HasValue ? "Customer updated successfully." : "Customer added successfully.";
        return RedirectToPage("/Index", new { area = "Customers" });
    }
}
