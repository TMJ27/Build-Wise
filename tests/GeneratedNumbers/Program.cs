using BuildWise.Data;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var options = new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
await using var db = new BuildWiseDbContext(options);
var date = new DateTime(2026, 1, 1);
var first = new User
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
    Name = "Zed", Email = "first@example.com", PasswordHash = "test", CreationDate = date
};
var second = new User
{
    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
    Name = "Amy", Email = "second@example.com", PasswordHash = "test", CreationDate = date
};
var third = new User
{
    Id = Guid.NewGuid(), Name = "Bob", Email = "third@example.com",
    PasswordHash = "test", CreationDate = date.AddDays(1)
};
db.Users.AddRange(third, second, first);
var group = new UserGroup
{
    Id = Guid.NewGuid(), Name = "Administrators", CreatorId = first.Id, CreationDate = date
};
db.UserGroups.Add(group);
second.GroupId = group.Id;
db.Currencies.AddRange(
    new Currency { Id = Guid.NewGuid(), Name = "Z dollar", Code = "ZZZ", Symbol = "$", CreatorId = first.Id, CreationDate = date },
    new Currency { Id = Guid.NewGuid(), Name = "A dollar", Code = "AAA", Symbol = "$", CreatorId = first.Id, CreationDate = date.AddDays(1) });
await db.SaveChangesAsync();
db.ChangeTracker.Clear();

// A filtered list must retain the complete table's numbering.
var subset = await db.Users.AsNoTracking().Where(user => user.Id != first.Id).OrderBy(user => user.Name).ToListAsync();
await db.ApplyUserNumbersAsync(subset);
Check(subset[0].UserNumber == "USR-0002" && subset[1].UserNumber == "USR-0003", "Filtered numbering or timestamp tie order changed.");
var details = new BuildWise.Web.Areas.Configuration.Pages.Users.DetailsModel(db);
await details.OnGet(second.Id);
Check(details.Person?.UserNumber == "USR-0002", "User details number differs from list.");
await db.ApplyUserGroupNumberAsync(group);
Check(group.UserGroupNumber == "UG-0001", "Group format is incorrect.");
var groupDetails = new BuildWise.Web.Areas.Configuration.Pages.UserGroups.DetailsModel(db);
await groupDetails.OnGetAsync(group.Id);
Check(groupDetails.GroupDetails.UserGroupNumber == "UG-0001", "Group details number is incorrect.");
Check(groupDetails.GroupDetails.Members.Single().UserNumber == "USR-0002", "Group members must use the global user number.");

var currencyIndex = new BuildWise.Web.Areas.Configuration.Pages.Currencies.IndexModel(db);
await currencyIndex.OnGetAsync();
Check(currencyIndex.Currencies[0].CurrencyNumber == "CUR-0002", "Currency sorting changed its global number.");
var currencyDetails = new BuildWise.Web.Areas.Configuration.Pages.Currencies.DetailsModel(db);
await currencyDetails.OnGetAsync(currencyIndex.Currencies[0].Id);
Check(currencyDetails.CurrencyDetails.CurrencyNumber == "CUR-0002", "Currency details number differs from list.");

Check(db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.UserSequence)) is null, "Display sequence was mapped to SQL.");
Check(db.Model.FindEntityType(typeof(UserGroup))!.FindProperty(nameof(UserGroup.UserGroupNumber)) is null, "Display number was mapped to SQL.");
Check(!db.ChangeTracker.HasChanges(), "Reading display numbers modified database records.");
Console.WriteLine("PASS: global numbering, filtered rows, timestamp ties, details consistency, currency projections and unmapped display fields.");
