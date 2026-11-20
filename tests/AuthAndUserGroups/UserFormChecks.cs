using BuildWise.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Form = BuildWise.Web.Areas.Configuration.Pages.Users.UpsertModel;

internal static class UserFormChecks
{
    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var creator = new User { Id = Guid.NewGuid(), Email = "staff@example.test", PasswordHash = "unused" };
        var group = new UserGroup { Id = Guid.NewGuid(), Name = "Office", Creator = creator };
        db.AddRange(creator, group);
        await db.SaveChangesAsync();
        Form Page(Guid? staffId) => new(db, new StubCurrentUser { Id = staffId })
        { TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData()) };
        var create = Page(creator.Id);
        Check(await create.OnGetAsync(null) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
            && create.Groups.Single().Value == group.Id.ToString(), "Create form must open and load actual groups.");
        create.Input.Name = "New staff";
        create.Input.Email = "new@example.test";
        create.Input.GroupId = group.Id;
        var result = await create.OnPostAsync(null);
        var saved = await db.Users.SingleAsync(user => user.Email == "new@example.test");
        Check(result is RedirectToPageResult { PageName: "/Users/Details" } redirect
            && (Guid)redirect.RouteValues!["id"]! == saved.Id && !string.IsNullOrEmpty(saved.PasswordHash)
            && saved.CreatorId == creator.Id, "Creation must save valid fields and open the new user's details.");
        var edit = Page(creator.Id);
        await edit.OnGetAsync(saved.Id);
        Check(edit.Input.Id == saved.Id && edit.Input.Name == saved.Name && edit.Input.GroupId == group.Id
            && saved.Name == "New staff", "Edit GET must populate inputs without changing the record.");
        Check(await edit.OnPostAsync(Guid.NewGuid()) is BadRequestResult, "Mismatched route and posted IDs must be rejected.");
        edit.Input.Email = creator.Email;
        Check(await edit.OnPostAsync(saved.Id) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
            && !edit.ModelState.IsValid && edit.Groups.Count == 1, "Duplicate edits must retain group options and show validation.");
        var missing = Page(creator.Id);
        missing.Input.Id = Guid.NewGuid();
        missing.Input.Email = "missing@example.test";
        Check(await missing.OnPostAsync(missing.Input.Id) is NotFoundResult, "Missing edit records must not be recreated.");
        Check(await Page(null).OnPostAsync(null) is UnauthorizedResult, "A creator is required before saving a user.");
        Console.WriteLine("PASS: user form create/edit loading, configured groups, details redirect, valid persistence and validation guards.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
