using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Services.Pages;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var empty = new IndexModel(db);
await empty.OnGetAsync();
Check(empty.TotalCount == 0 && empty.Records.Count == 0, "Empty catalogue summaries must be zero.");

var creator = new User { Id = Guid.NewGuid(), Email = "test@example.test", PasswordHash = "test", CreationDate = DateTime.UtcNow };
var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Plumbing", IsActive = false, CreationDate = DateTime.UtcNow };
var first = new Service { Id = Guid.NewGuid(), Name = "Zulu", Creator = creator, Category = category, IsActive = true, RequireSiteAssessment = true, CreationDate = new DateTime(2026, 1, 1) };
var second = new Service { Id = Guid.NewGuid(), Name = "Alpha", Creator = creator, Category = category, IsActive = false, CreationDate = new DateTime(2026, 1, 2) };
db.AddRange(creator, category, first, second);
await db.SaveChangesAsync();

var active = new IndexModel(db);
await active.OnGetAsync();
Check(active.Records.Single().Status == ActiveStatus.Active, "Default filter must show active services with enum status.");
Check(active.ActiveCount == 1 && active.InactiveCount == 1 && active.AssessmentCount == 1 && active.CategoryCount == 1, "Summary queries must count real records.");
Check(active.Categories.Single().Text == "Plumbing (" + ActiveStatus.Inactive + ")", "Category labels must use the shared status enum.");

var inactive = new IndexModel(db) { Status = StatusFilter.Inactive };
await inactive.OnGetAsync();
Check(inactive.Records.Single().Status == ActiveStatus.Inactive && inactive.Records[0].Id == second.Id, "Inactive enum filter must show inactive records.");
var all = new IndexModel(db) { Status = StatusFilter.All };
await all.OnGetAsync();
Check(all.Records.Count == 2 && all.Records[0].Name == "Alpha", "All must include both statuses and preserve name sorting.");

var filtered = new IndexModel(db) { Status = StatusFilter.All, Search = "zulu", CategoryId = category.Id };
await filtered.OnGetAsync();
Check(filtered.Records.Single().Id == first.Id && filtered.TotalCount == 2 && filtered.InactiveCount == 1, "Filters must preserve global IDs and catalogue-wide summary counts.");
await db.ApplyServiceNumberAsync(first);
Check(filtered.Records.Single().ServiceNumber == first.ServiceNumber, "Filtered services must use the entity's generated display number.");
var missing = new IndexModel(db) { Status = StatusFilter.All, Search = "missing" };
await missing.OnGetAsync();
Check(missing.Records.Count == 0, "Text search must tolerate null descriptions.");
var invalid = new IndexModel(db) { Status = (StatusFilter)999 };
await invalid.OnGetAsync();
Check(invalid.Status == StatusFilter.Active && invalid.Records.Single().Id == first.Id, "Unknown enum values must fall back to Active.");
var malformed = new IndexModel(db);
malformed.ModelState.AddModelError(nameof(IndexModel.Status), "Invalid query value");
await malformed.OnGetAsync();
Check(malformed.ModelState.IsValid && malformed.Status == StatusFilter.Active, "Malformed filter input must be cleared and normalised.");
Console.WriteLine("PASS: enum statuses, active/inactive/all filtering, invalid values, category labels, SQL-style summaries, search and global display numbers.");
