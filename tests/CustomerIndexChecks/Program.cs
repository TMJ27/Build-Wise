using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Customers.Pages;
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
Check(empty.ClientList.Count == 0 && empty.TotalCount == 0 && empty.OpenCount == 0, "An empty register must have zero counts.");

var creator = new User { Id = Guid.NewGuid(), Email = "creator@example.test", PasswordHash = "test", CreationDate = DateTime.UtcNow };
var date = new DateTime(2026, 1, 1);
var first = new Client { Id = Guid.NewGuid(), Name = "Zulu", Email = "zulu@example.test", Address = "Harare", ClientTypeId = (int)ClientType.Business, IsActive = true, Creator = creator, CreationDate = date };
var second = new Client { Id = Guid.NewGuid(), Name = "Alpha", Email = "alpha@example.test", Address = "Bulawayo", ClientTypeId = (int)ClientType.Individual, Creator = creator, CreationDate = date.AddDays(1) };
var third = new Client { Id = Guid.NewGuid(), Name = "Beta", Email = "beta@example.test", Address = "Harare", ClientTypeId = (int)ClientType.Business, Creator = creator, CreationDate = date.AddDays(2) };
db.AddRange(creator, first, second, third);
db.ClientRequests.AddRange(
    new ClientRequest { Id = Guid.NewGuid(), Client = first, Title = "First request", SiteAddress = "Harare", StatusId = (int)ClientRequestStatus.Open, Creator = creator, CreationDate = date },
    new ClientRequest { Id = Guid.NewGuid(), Client = first, Title = "Second request", SiteAddress = "Harare", StatusId = (int)ClientRequestStatus.Open, Creator = creator, CreationDate = date },
    new ClientRequest { Id = Guid.NewGuid(), Client = second, Title = "Closed request", SiteAddress = "Bulawayo", StatusId = (int)ClientRequestStatus.Closed, Creator = creator, CreationDate = date });
await db.SaveChangesAsync();

var index = new IndexModel(db);
await index.OnGetAsync();
Check(index.TotalCount == 3 && index.BusinessCount == 2 && index.IndividualsCount == 1, "Summary cards must count actual client types.");
Check(index.OpenCount == 1, "A client with two open requests must be counted only once.");
Check(index.ClientList.First().Name == "Alpha" && index.ClientList.First().ClientSequence == 2, "Name sorting must preserve global creation-order numbering.");

var typeFilter = new IndexModel(db) { Type = ((int)ClientType.Individual).ToString() };
await typeFilter.OnGetAsync();
Check(typeFilter.ClientList.Single().Id == second.Id && typeFilter.TotalCount == 3 && typeFilter.OpenCount == 1, "Type filters must not change catalogue-wide card counts.");
var search = new IndexModel(db) { Search = "  ZULU " };
await search.OnGetAsync();
Check(search.ClientList.Single().Id == first.Id && search.Search == "ZULU", "Search must be trimmed, case-insensitive and tolerate null phone values.");
var address = new IndexModel(db) { Search = "bulawayo" };
await address.OnGetAsync();
Check(address.ClientList.Single().Id == second.Id, "Address search must return matching clients.");
var newest = new IndexModel(db) { Sort = "newest" };
await newest.OnGetAsync();
Check(newest.ClientList.First().Id == third.Id, "Newest sorting is incorrect.");
var oldest = new IndexModel(db) { Sort = "oldest" };
await oldest.OnGetAsync();
Check(oldest.ClientList.First().Id == first.Id, "Oldest sorting is incorrect.");
var invalid = new IndexModel(db) { Type = "999", Sort = "invalid" };
await invalid.OnGetAsync();
Check(invalid.Type == "all" && invalid.Sort == "name" && invalid.ClientList.Count == 3, "Unknown filters must fall back to the register defaults.");
var missing = new IndexModel(db) { Search = "missing client" };
await missing.OnGetAsync();
Check(missing.ClientList.Count == 0 && missing.TotalCount == 3, "An empty search result must retain the real totals.");
Check(await db.GetClientSequenceAsync(first) == 1, "Clients must use the shared generated-number helper.");
Check(db.Model.FindEntityType(typeof(Client))!.FindProperty(nameof(Client.ClientSequence)) is null, "Display numbers must not add a database column.");
for (var number = 0; number < 12; number++)
{
    db.Clients.Add(new Client
    {
        Id = Guid.NewGuid(), Name = $"Extra {number:D2}", Email = $"extra{number}@example.test",
        Address = "Harare", ClientTypeId = (int)ClientType.Business,
        Creator = creator, CreationDate = date.AddDays(number + 3)
    });
}
await db.SaveChangesAsync();
var page = new IndexModel(db) { Sort = "oldest", PageNumber = 2 };
await page.OnGetAsync();
Check(page.FilteredCount == 15 && page.PageCount == 2 && page.ClientList.Count == 5, "Pagination must count all matches and load only the requested page.");
Check(page.ClientList.First().ClientSequence == 11 && page.TotalCount == 15, "Paging must preserve global display numbers and summary counts.");
var pastEnd = new IndexModel(db) { PageNumber = int.MaxValue };
await pastEnd.OnGetAsync();
Check(pastEnd.PageNumber == 2 && pastEnd.ClientList.Count == 5, "An out-of-range page must use the final available page.");
var filteredPage = new IndexModel(db) { Type = "1", PageNumber = -2 };
await filteredPage.OnGetAsync();
Check(filteredPage.PageNumber == 1 && filteredPage.FilteredCount == 1 && filteredPage.ClientList.Single().Id == second.Id, "Filters and page bounds must work together.");
Console.WriteLine("PASS: customer summaries, open-request counts, filters, search, sorting, pagination and global display numbers.");

