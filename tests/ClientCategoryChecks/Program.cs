using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.Pages.ClientCategories;
using BuildWise.Web.Areas.Configuration.ViewModels.ClientCategories;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static UpsertModel Form(BuildWiseDbContext db, Guid? userId, ClientCategoryUpsertViewModel input)
{
    var page = new UpsertModel(db, new CurrentUser(userId))
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
Check(empty.Categories.Count == 0, "Empty register must not show sample categories.");
var creator = new User { Id = Guid.NewGuid(), Name = "Test", Surname = "Creator", Email = "creator@example.test", PasswordHash = "test", CreationDate = DateTime.UtcNow };
db.Users.Add(creator);
await db.SaveChangesAsync();

var create = Form(db, creator.Id, new() { Name = " Individuals ", Description = " Personal clients " });
Check(await create.OnPostAsync(null) is RedirectToPageResult { PageName: "./Details" }, "Create must save and open Details.");
var category = await db.ClientCategories.SingleAsync();
var creationDate = category.CreationDate;
Check(category.Name == "Individuals" && category.Description == "Personal clients" && category.CreatorId == creator.Id,
    "Category must save trimmed values and the current creator.");
Check(category.CreationDate.Kind == DateTimeKind.Utc && category.LastUpdated is null, "Creation audit fields must be set correctly.");
Check(create.TempData["SuccessMessage"] is string, "Create must set a success message.");

var duplicate = Form(db, creator.Id, new() { Name = "individuals" });
Check(await duplicate.OnPostAsync(null) is PageResult && !duplicate.ModelState.IsValid, "Duplicate names must be rejected irrespective of case.");
foreach (var name in new[] { "", "   ", new string('a', 51) })
{
    var invalid = Form(db, creator.Id, new() { Name = name });
    Check(await invalid.OnPostAsync(null) is PageResult && !invalid.ModelState.IsValid, "Required name and database length limits must be validated.");
}
Check(await Form(db, null, new() { Name = "Unauthorised" }).OnPostAsync(null) is UnauthorizedResult,
    "Creation without a current local user must be refused.");
Check(await Form(db, creator.Id, new() { Id = category.Id, Name = "Mismatch" }).OnPostAsync(Guid.NewGuid()) is BadRequestResult,
    "Route and posted IDs must match.");
var missingId = Guid.NewGuid();
Check(await new UpsertModel(db, new CurrentUser(creator.Id)).OnGetAsync(missingId) is NotFoundResult,
    "Missing edit GET must return 404.");
Check(await Form(db, creator.Id, new() { Id = missingId, Name = "Missing" }).OnPostAsync(missingId) is NotFoundResult,
    "Missing edits must never create a new record.");

var edit = new UpsertModel(db, new CurrentUser(creator.Id));
await edit.OnGetAsync(category.Id);
Check(edit.Input.Name == category.Name && edit.Input.Description == category.Description, "Edit must populate saved values.");
var update = Form(db, Guid.NewGuid(), new() { Id = category.Id, Name = "Individuals", Description = "   " });
Check(await update.OnPostAsync(category.Id) is RedirectToPageResult, "Edit must save and redirect.");
Check(category.Description is null && category.CreatorId == creator.Id && category.CreationDate == creationDate && category.LastUpdated.HasValue,
    "Edit must accept a blank description, preserve creation fields and record update time.");

var business = new ClientCategory { Id = Guid.NewGuid(), Name = "Businesses", CreatorId = creator.Id, CreationDate = creationDate.AddDays(1) };
db.ClientCategories.Add(business);
db.Clients.AddRange(
    new Client { Id = Guid.NewGuid(), Name = "Client one", Email = "one@example.test", Address = "Harare", ClientTypeId = category.Id, CreatorId = creator.Id, CreationDate = DateTime.UtcNow },
    new Client { Id = Guid.NewGuid(), Name = "Client two", Email = "two@example.test", Address = "Harare", ClientTypeId = category.Id, CreatorId = creator.Id, CreationDate = DateTime.UtcNow });
await db.SaveChangesAsync();

var index = new IndexModel(db);
await index.OnGetAsync();
Check(index.Categories[0].Name == "Businesses" && index.Categories[0].CategoryNumber == "CCAT-0002",
    "Name sorting must not change whole-table display numbers.");
Check(index.Categories.Single(item => item.Id == category.Id).ClientCount == 2 && index.Categories[0].ClientCount == 0,
    "Client counts must use the corrected ClientTypeId relationship.");
var details = new DetailsModel(db);
await details.OnGetAsync(category.Id);
Check(details.CategoryDetails.ClientCount == 2 && details.CategoryDetails.CreatorName == "Test Creator"
    && details.CategoryDetails.CategoryNumber == "CCAT-0001", "Details must show matching counts, number and creator.");
Check(await details.OnGetAsync(missingId) is NotFoundResult, "Missing Details must return 404.");
Check(await db.GetClientCategorySequenceAsync(business) == 2, "Category numbering must use the shared helper.");
Check(db.Model.FindEntityType(typeof(ClientCategory))!.FindProperty(nameof(ClientCategory.CategorySequence)) is null,
    "Display numbers must not require new database columns.");
var foreignKey = db.Model.FindEntityType(typeof(Client))!.GetForeignKeys().Single(key => key.Properties.Any(property => property.Name == nameof(Client.ClientTypeId)));
Check(foreignKey.PrincipalEntityType.ClrType == typeof(ClientCategory), "ClientTypeId must reference ClientCategory.");

await using var sqlModel = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseSqlServer("Server=localhost;Database=MetadataOnly;Integrated Security=True;TrustServerCertificate=True").Options);
Check(!sqlModel.Database.HasPendingModelChanges(), "No new schema migration should be required after the user's relationship migration.");
Console.WriteLine("PASS: client category CRUD, validation, creator/audit preservation, relationship, counts, global numbering and migration consistency. No real database was changed.");

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
