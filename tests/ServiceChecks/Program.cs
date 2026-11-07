using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Services.Pages;
using BuildWise.Web.Areas.Services.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static UpsertModel Form(BuildWiseDbContext db, Guid? creatorId, ServiceUpsertViewModel input)
{
    var page = new UpsertModel(db, new CurrentUser(creatorId))
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
Check(empty.TotalCount == 0 && empty.Records.Count == 0, "Empty catalogue must not show sample services.");
var creator = new User { Id = Guid.NewGuid(), Name = "Test", Surname = "Owner", Email = "owner@example.test", PasswordHash = "test", CreationDate = DateTime.UtcNow };
var plumbing = new ServiceCategory { Id = Guid.NewGuid(), Name = "Plumbing", IsActive = true, CreationDate = DateTime.UtcNow };
var electrical = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrical", IsActive = true, CreationDate = DateTime.UtcNow };
var inactive = new ServiceCategory { Id = Guid.NewGuid(), Name = "Old category", IsActive = false, CreationDate = DateTime.UtcNow };
db.Users.Add(creator);
db.ServiceCategories.AddRange(plumbing, electrical, inactive);
await db.SaveChangesAsync();

var create = Form(db, creator.Id, new() { Name = " Zulu repairs ", CategoryId = plumbing.Id, Description = " Pipe repairs ", RequireSiteAssessment = true });
Check(await create.OnPostAsync(null) is RedirectToPageResult { PageName: "./Details" }, "New service must save and redirect to details.");
var service = await db.Services.SingleAsync();
Check(service.Name == "Zulu repairs" && service.Description == "Pipe repairs", "Service inputs must be trimmed.");
Check(service.CreatorId == creator.Id && service.CategoryId == plumbing.Id && service.CreationDate.Kind == DateTimeKind.Utc, "Creator, category and UTC creation date must be saved.");
var creationDate = service.CreationDate;
Check(create.TempData["SuccessMessage"] is string, "Successful saves must show a message.");
var details = new DetailsModel(db);
await details.OnGetAsync(service.Id);
Check(details.ServiceDetails.CreatorName == "Test Owner" && details.ServiceDetails.CategoryName == "Plumbing"
    && details.ServiceDetails.ServiceNumber == "SRV-0001", "Details must show real relationships and display number.");

var duplicate = Form(db, creator.Id, new() { Name = "zulu repairs", CategoryId = plumbing.Id });
Check(await duplicate.OnPostAsync(null) is PageResult && !duplicate.ModelState.IsValid, "Duplicate names within a category must be rejected.");
var badCategory = Form(db, creator.Id, new() { Name = "Invalid", CategoryId = Guid.NewGuid() });
Check(await badCategory.OnPostAsync(null) is PageResult && !badCategory.ModelState.IsValid, "Unknown categories must be rejected before saving.");
var inactiveCategory = Form(db, creator.Id, new() { Name = "Invalid", CategoryId = inactive.Id });
Check(await inactiveCategory.OnPostAsync(null) is PageResult && !inactiveCategory.ModelState.IsValid, "New services cannot select inactive categories.");
var anonymous = Form(db, null, new() { Name = "Anonymous", CategoryId = plumbing.Id });
Check(await anonymous.OnPostAsync(null) is UnauthorizedResult, "New services require a current creator.");
var invalid = Form(db, creator.Id, new() { Name = "", CategoryId = null });
Check(await invalid.OnPostAsync(null) is PageResult && invalid.Categories.Count == 2, "Validation errors must retain category options.");
Check(await Form(db, creator.Id, new() { Id = service.Id, Name = "Wrong", CategoryId = plumbing.Id }).OnPostAsync(Guid.NewGuid()) is BadRequestResult,
    "Route and posted IDs must match.");
var missingId = Guid.NewGuid();
Check(await Form(db, creator.Id, new() { Id = missingId, Name = "Missing", CategoryId = plumbing.Id }).OnPostAsync(missingId) is NotFoundResult,
    "Missing edit records must never be recreated.");

var edit = Form(db, creator.Id, new() { Id = service.Id, Name = "Zulu repairs", CategoryId = plumbing.Id, Description = "Updated", RequireSiteAssessment = true });
Check(await edit.OnPostAsync(service.Id) is RedirectToPageResult, "Existing service must update.");
Check(service.CreatorId == creator.Id && service.CreationDate == creationDate && service.LastUpdated.HasValue, "Edits must preserve creation fields and set update time.");

// Add later records whose names sort before the first service.
db.Services.AddRange(
    new Service { Id = Guid.NewGuid(), Name = "Alpha", CategoryId = electrical.Id, CreatorId = creator.Id, CreationDate = creationDate.AddDays(1), IsActive = false },
    new Service { Id = Guid.NewGuid(), Name = "Beta", CategoryId = electrical.Id, CreatorId = creator.Id, CreationDate = creationDate.AddDays(2), IsActive = true });
await db.SaveChangesAsync();
var index = new IndexModel(db);
await index.OnGetAsync();
Check(index.TotalCount == 3 && index.ActiveCount == 2 && index.InactiveCount == 1 && index.AssessmentCount == 1 && index.CategoryCount == 2, "Summary counts must use actual database records.");
Check(index.Records.Count == 2 && index.Records.Single(row => row.Id == service.Id).ServiceNumber == "SRV-0001", "Alphabetic sorting must not change service numbers.");
var filtered = new IndexModel(db) { CategoryId = plumbing.Id, Search = "zulu", Status = StatusFilter.All };
await filtered.OnGetAsync();
Check(filtered.Records.Count == 1 && filtered.Records[0].Id == service.Id, "Category and case-insensitive name search must combine correctly.");
Check(filtered.TotalCount == 3 && filtered.ActiveCount == 2 && filtered.InactiveCount == 1 && filtered.CategoryCount == 2,
    "Table filters must not change the catalogue-wide summary cards.");
var descriptionSearch = new IndexModel(db) { Search = "UPDATED", Status = StatusFilter.All };
await descriptionSearch.OnGetAsync();
Check(descriptionSearch.Records.Single().Id == service.Id, "Description search must be case insensitive and tolerate null descriptions.");
var missingService = new IndexModel(db) { Search = "missing service", Status = StatusFilter.All };
await missingService.OnGetAsync();
Check(missingService.Records.Count == 0, "Unknown service names must show an empty result.");
var inactiveIndex = new IndexModel(db) { Status = StatusFilter.Inactive };
await inactiveIndex.OnGetAsync();
Check(inactiveIndex.Records.Single().Name == "Alpha", "Inactive filter must exclude active records.");

details.TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData());
Check(await details.OnPostToggleAsync(service.Id) is RedirectToPageResult && !service.IsActive, "Deactivation must update the database and redirect.");
Check(service.CreatorId == creator.Id && service.CreationDate == creationDate, "Status changes must preserve creation fields.");
await details.OnPostToggleAsync(service.Id);
Check(service.IsActive, "Activation must restore availability.");
Check(await details.OnGetAsync(missingId) is NotFoundResult && await details.OnPostToggleAsync(missingId) is NotFoundResult, "Missing details and status changes must return 404.");
Check(db.Model.FindEntityType(typeof(Service))!.FindProperty(nameof(Service.ServiceSequence)) is null, "Display numbers must not require database changes.");
foreach (var description in new string?[] { null, "", "   " })
{
    var name = "Optional description " + await db.Services.CountAsync();
    var optional = Form(db, creator.Id, new() { Name = name, CategoryId = plumbing.Id, Description = description });
    Check(await optional.OnPostAsync(null) is RedirectToPageResult, "Blank descriptions must not crash service creation.");
    var optionalService = await db.Services.SingleAsync(item => item.Name == name);
    Check(optionalService.Description is null, "Blank descriptions must be saved as null.");
    var optionalEdit = Form(db, creator.Id, new() { Id = optionalService.Id, Name = name, CategoryId = plumbing.Id, Description = description });
    Check(await optionalEdit.OnPostAsync(optionalService.Id) is RedirectToPageResult, "Blank descriptions must not crash service edits.");
}
Console.WriteLine("PASS: service creation/editing, creator tracking, category validation, duplicate and ID checks, summaries, filters, numbering, details and activation/deactivation.");

sealed class MemoryTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}

sealed class CurrentUser(Guid? id) : ICurrentUserService
{
    public Task<Guid?> GetUserIdAsync() => Task.FromResult(id);
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
}
