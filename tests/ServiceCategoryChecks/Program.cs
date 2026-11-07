using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildWise.Lib.Users;
using BuildWise.Data;
using BuildWise.Web.Areas.Configuration.Pages.ServiceCategories;
using BuildWise.Web.Areas.Configuration.ViewModels.ServiceCategories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static UpsertModel Form(BuildWiseDbContext db, ServiceCategoryUpsertViewModel input)
{
    var page = new UpsertModel(db)
    {
        Input = input,
        TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
    };
    var errors = new List<ValidationResult>();
    Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
    foreach (var error in errors)
    {
        page.ModelState.AddModelError("Input." + error.MemberNames.FirstOrDefault(), error.ErrorMessage!);
    }
    return page;
}

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

var empty = new IndexModel(db);
await empty.OnGetAsync();
Check(empty.Categories.Count == 0, "Empty database must show an empty register.");
var create = Form(db, new() { Name = " Plumbing ", Description = " Repairs ", IsActive = true });
Check(await create.OnPostAsync(null) is RedirectToPageResult { PageName: "./Details" }, "Create must redirect to details.");
var saved = await db.ServiceCategories.SingleAsync();
var createdAt = saved.CreationDate;
Check(saved.Name == "Plumbing" && saved.Description == "Repairs" && saved.Id != Guid.Empty, "Create did not trim and save the inputs.");
Check(saved.LastUpdated is null && create.TempData["SuccessMessage"] is string, "Create must set a success message and creation date only.");

var duplicate = Form(db, new() { Name = "plumbing" });
Check(await duplicate.OnPostAsync(null) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult
    && !duplicate.ModelState.IsValid && await db.ServiceCategories.CountAsync() == 1,
    "Duplicate names must be refused regardless of case.");
var blank = Form(db, new() { Name = "   " });
Check(await blank.OnPostAsync(null) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult, "Blank names must be refused.");
var tooLong = Form(db, new() { Name = new string('a', 51) });
Check(await tooLong.OnPostAsync(null) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult, "Database name limit must be validated.");
var tampered = Form(db, new() { Id = saved.Id, Name = "Tampered" });
Check(await tampered.OnPostAsync(Guid.NewGuid()) is BadRequestResult, "Route and input IDs must match.");
var missingId = Guid.NewGuid();
Check(await new UpsertModel(db).OnGetAsync(missingId) is NotFoundResult, "Missing edit must return 404.");
Check(await new DetailsModel(db).OnGetAsync(missingId) is NotFoundResult, "Missing details must return 404.");
Check(await Form(db, new() { Id = missingId, Name = "Missing" }).OnPostAsync(missingId) is NotFoundResult,
    "Missing edits must never create replacement records.");

var user = new User { Id = Guid.NewGuid(), Email = "test@example.test", PasswordHash = "test", CreationDate = DateTime.UtcNow };
db.Users.Add(user);
var service = new Service { Id = Guid.NewGuid(), Name = "Pipe repairs", CategoryId = saved.Id, CreatorId = user.Id, CreationDate = DateTime.UtcNow, IsActive = true };
db.Services.Add(service);
var other = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrical", CreationDate = createdAt.AddDays(1), IsActive = true };
db.ServiceCategories.Add(other);
await db.SaveChangesAsync();

var edit = new UpsertModel(db);
Check(await edit.OnGetAsync(saved.Id) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult && edit.Input.Name == "Plumbing", "Edit must populate saved values.");
var update = Form(db, new() { Id = saved.Id, Name = "Plumbing", IsActive = false });
Check(await update.OnPostAsync(saved.Id) is RedirectToPageResult, "Update must save and redirect.");
Check(saved.CreationDate == createdAt && saved.LastUpdated.HasValue && !saved.IsActive, "Update must preserve creation date and record the update date.");
Check(service.CategoryId == saved.Id, "Deactivating a category must preserve existing services.");

var index = new IndexModel(db);
await index.OnGetAsync();
Check(index.Categories[0].Name == "Electrical" && index.Categories[0].CategoryNumber == "CAT-0002", "Sorting must not change global category numbering.");
Check(index.Categories.Single(row => row.Id == saved.Id).ServiceCount == 1, "Register must count actual services.");
var details = new DetailsModel(db);
await details.OnGetAsync(saved.Id);
Check(details.CategoryDetails.CategoryNumber == "CAT-0001" && details.CategoryDetails.ServiceCount == 1, "Details must match register numbering and counts.");

var addService = new BuildWise.Web.Areas.Services.Pages.UpsertModel(db, new NoCurrentUser());
await addService.OnGetAsync(null);
Check(addService.Categories.Count == 1 && addService.Categories[0].Value == other.Id.ToString(), "New services must use active categories only.");
var editService = new BuildWise.Web.Areas.Services.Pages.UpsertModel(db, new NoCurrentUser());
await editService.OnGetAsync(service.Id);
Check(editService.Input.CategoryId == saved.Id && editService.Categories.Any(option => option.Value == saved.Id.ToString() && option.Text.Contains(nameof(BuildWise.Lib.Enums.Statuses.ActiveStatus.Inactive))),
    "Existing services must retain their inactive category.");
Check(db.Model.FindEntityType(typeof(ServiceCategory))!.FindProperty(nameof(ServiceCategory.CategorySequence)) is null,
    "Display numbering must not require a schema change.");
Console.WriteLine("PASS: category creation, validation, duplicate checks, editing, status, audit dates, ID safety, register/details and service dropdown integration.");

sealed class MemoryTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}

sealed class NoCurrentUser : ICurrentUserService
{
    public Task<Guid?> GetUserIdAsync() => Task.FromResult<Guid?>(null);
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
}
