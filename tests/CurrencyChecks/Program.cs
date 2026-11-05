using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.Pages.Currencies;
using BuildWise.Web.Areas.Configuration.ViewModels.Currency;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var creator = new User
{
    Id = Guid.NewGuid(), Name = "Currency", Surname = "Creator",
    Email = "creator@example.test", PasswordHash = "unused", IsActive = true
};
db.Users.Add(creator);
await db.SaveChangesAsync();
var currentUser = new TestCurrentUser { Id = creator.Id };

UpsertModel Form(string code, Guid? id = null)
{
    var http = new DefaultHttpContext();
    return new UpsertModel(db, currentUser)
    {
        PageContext = new PageContext { HttpContext = http },
        TempData = new TempDataDictionary(http, new TestTempData()),
        Input = new CurrencyInputViewModel { Id = id, Name = "US Dollar", Code = code, Symbol = "$" }
    };
}

var create = Form("usd");
Check(await create.OnGetAsync(null) is PageResult, "Create GET should open.");
Check(await create.OnPostAsync(null) is RedirectToPageResult, "Create should redirect.");
var saved = await db.Currencies.SingleAsync();
Check(saved.Code == "USD" && saved.CreatorId == creator.Id, "Normalize code and assign actual creator.");
var creationDate = saved.CreationDate;
Check(creationDate.Kind == DateTimeKind.Utc, "Creation date should use UTC.");
Check((string?)create.TempData["SuccessMessage"] == "Currency created successfully.", "Create message.");

var duplicate = Form("usd");
Check(await duplicate.OnPostAsync(null) is PageResult
    && duplicate.ModelState.ContainsKey("Input.Code"), "Duplicate code should produce a field error.");
Check(await db.Currencies.CountAsync() == 1, "Duplicate must not insert.");

currentUser.Id = Guid.NewGuid();
var edit = Form("usd", saved.Id);
edit.Input.Name = "United States Dollar";
Check(await edit.OnPostAsync(saved.Id) is RedirectToPageResult, "Same code on same row can be saved.");
Check(saved.Name == edit.Input.Name && saved.CreatorId == creator.Id
    && saved.CreationDate == creationDate, "Edit should preserve creator and creation date.");
var load = Form("", saved.Id);
Check(await load.OnGetAsync(saved.Id) is PageResult && load.Input.Code == "USD", "Edit GET populates input.");

var missingId = Guid.NewGuid();
Check(await Form("EUR", missingId).OnPostAsync(missingId) is NotFoundResult, "Missing edit must not insert.");
Check(await Form("EUR").OnPostAsync(saved.Id) is BadRequestResult, "Reject posted ID mismatch.");
var invalid = Form("EUR");
invalid.ModelState.AddModelError("Input.Name", "Required");
Check(await invalid.OnPostAsync(null) is PageResult, "Validation errors should redisplay form.");
currentUser.Id = null;
var anonymous = Form("EUR");
Check(await anonymous.OnPostAsync(null) is PageResult && !anonymous.ModelState.IsValid,
    "Missing current user cannot create a currency.");
Check(await db.Currencies.CountAsync() == 1, "Rejected requests must not insert.");

var index = new IndexModel(db);
await index.OnGetAsync();
Check(index.Currencies.Count == 1 && index.Currencies[0].Id == saved.Id, "Index returns actual rows.");
var details = new DetailsModel(db);
Check(await details.OnGetAsync(saved.Id) is PageResult
    && details.CurrencyDetails.Creator == "Currency Creator", "Details should include creator.");
Check(await details.OnGetAsync(Guid.NewGuid()) is NotFoundResult, "Missing details returns 404.");

Console.WriteLine("Passed: currency create/edit, creator preservation, duplicate codes, validation, missing IDs, index and details. Used only an isolated in-memory database.");

sealed class TestCurrentUser : ICurrentUserService
{
    public Guid? Id { get; set; }
    public Task<Guid?> GetUserIdAsync() => Task.FromResult(Id);
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
}

sealed class TestTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
