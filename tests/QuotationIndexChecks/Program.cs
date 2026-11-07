using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Quotations.Pages;
using Microsoft.EntityFrameworkCore;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var empty = new IndexModel(db);
await empty.OnGetAsync();
Check(empty.Quotations.Count == 0 && empty.Summary.TotalCount == 0, "Empty register must load.");

var user = new User { Id = Guid.NewGuid(), Email = "creator@example.test", PasswordHash = "test" };
var client = new Client { Id = Guid.NewGuid(), Name = "Current customer", Email = "client@example.test", Address = "Harare", Creator = user };
var request = new ClientRequest { Id = Guid.NewGuid(), Client = client, Title = "Request title", SiteAddress = "Harare", Creator = user };
Quotation Quote(string number)
{
    var quote = new Quotation { Id = Guid.NewGuid(), QuotationNumber = number, Request = request, Creator = user };
    db.Quotations.Add(quote);
    return quote;
}
QuotationRevision Revision(Quotation quote, int number, decimal? total, bool priced = true)
{
    var revision = new QuotationRevision
    {
        Id = Guid.NewGuid(), Quotation = quote, RevisionNumber = number,
        Title = "Revision " + number, CustomerNameSnapshot = "Snapshot customer",
        CompanyNameSnapshot = "BuildWise", SiteAddressSnapshot = "Harare", RequestDescriptionSnapshot = "Work",
        Creator = user, CreationDate = new DateTime(2026, 1, 1).AddDays(number),
        StatusId = (int)QuotationStatus.Draft, IncludesPrices = priced, Total = total, CurrencyCode = "USD"
    };
    db.QuotationRevisions.Add(revision);
    return revision;
}
var first = Quote("QT-001");
Revision(first, 1, 100m).StatusId = (int)QuotationStatus.Approved;
var latest = Revision(first, 2, 250m);
// Revision number, rather than creation date, defines the latest revision.
latest.CreationDate = new DateTime(2025, 1, 1);
var unpriced = Quote("QT-002");
Revision(unpriced, 1, 999m, false);
var noRevision = Quote("QT-003");
var zero = Quote("QT-004");
Revision(zero, 1, 0m);
Revision(Quote("QT-005"), 1, null);
await db.SaveChangesAsync();

var index = new IndexModel(db);
await index.OnGetAsync();
var row = index.Quotations.Single(q => q.Id == first.Id);
Check(row.Total == 250m && row.RevisionNumber == 2 && row.CurrencyCode == "USD", "Amount must come from the latest revision without summing historical totals.");
Check(row.Title == latest.Title && row.CustomerName == latest.CustomerNameSnapshot, "Revision snapshots must be used.");
Check(row.CreatorName == user.Email, "Creator without a name must fall back to email.");
Check(index.Summary.TotalCount == 5 && index.Summary.DraftCount == 4 && index.Summary.ApprovedCount == 0 && index.Summary.PricedCount == 2, "Summaries must use latest revisions and count zero as priced.");
Check(index.Quotations.Single(q => q.Id == noRevision.Id).Total == null, "Quotation without revisions must remain visible without an amount.");
Check(!index.Quotations.Single(q => q.Id == unpriced.Id).IncludesPrices, "Unpriced revisions must retain their pricing flag.");
var filtered = new IndexModel(db) { Search = " qt-001 ", Status = QuotationStatus.Draft };
await filtered.OnGetAsync();
Check(filtered.Quotations.Single().Id == first.Id && filtered.Summary.TotalCount == 5, "Search and status must not change summary totals.");
var approved = new IndexModel(db) { Status = QuotationStatus.Approved };
await approved.OnGetAsync();
Check(approved.FilteredCount == 0, "An older approved revision must not match the latest status filter.");
for (var i = 0; i < 8; i++) Revision(Quote($"EXTRA-{i}"), 1, i);
await db.SaveChangesAsync();
var page = new IndexModel(db) { PageNumber = int.MaxValue, Sort = "oldest" };
await page.OnGetAsync();
Check(page.PageNumber == 2 && page.FilteredCount == 13 && page.Quotations.Count == 3, "Paging must clamp to the last page.");
Console.WriteLine("PASS: latest revision amounts, snapshots, summaries, search, status and pagination.");
await QuotationUpsertChecks.RunAsync();
await ScopeChecks.RunAsync();
await RequestChecks.RunAsync();
await OverviewChecks.RunAsync();
