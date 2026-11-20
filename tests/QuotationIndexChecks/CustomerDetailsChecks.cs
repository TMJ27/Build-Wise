using BuildWise.Data;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Customers.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

internal static class CustomerDetailsChecks
{
    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User { Id = Guid.NewGuid(), Email = "staff@example.test", PasswordHash = "test" };
        Client Client(string name) => new()
        {
            Id = Guid.NewGuid(), Name = name, Email = "client@example.test", Address = "Site",
            ClientTypeId = (int)ClientType.Business, Creator = user, CreationDate = new DateTime(2026, 11, 1)
        };
        var customer = Client("Saved customer");
        var other = Client("Other customer");
        ClientRequest Request(Client client, ClientRequestStatus status) => new()
        {
            Id = Guid.NewGuid(), Client = client, Title = "Saved request", ReferenceNumber = "REQ-20261101-0001",
            SiteAddress = "Site", Creator = user, StatusId = (int)status
        };
        var request = Request(customer, ClientRequestStatus.Open);
        var hiddenDraft = Request(customer, ClientRequestStatus.Draft);
        var otherRequest = Request(other, ClientRequestStatus.Open);
        Quotation Quote(ClientRequest source, string currency, decimal amount) => new()
        {
            Id = Guid.NewGuid(), Request = source, Creator = user, QuotationNumber = "legacy",
            QuotationRevisions = [Revision(1, QuotationStatus.Approved, currency, amount)]
        };
        QuotationRevision Revision(int number, QuotationStatus status, string currency, decimal amount) => new()
        {
            Id = Guid.NewGuid(), RevisionNumber = number, StatusId = (int)status, Title = "Saved quote",
            CompanyNameSnapshot = "Company", CustomerNameSnapshot = customer.Name, SiteAddressSnapshot = "Site",
            RequestDescriptionSnapshot = "Work", Creator = user, CreationDate = new DateTime(2026, 11, 2),
            IncludesPrices = true, CurrencyCode = currency, Total = amount
        };
        var usd = Quote(request, "USD", 100);
        usd.QuotationRevisions.Add(Revision(2, QuotationStatus.Draft, "USD", 999));
        var zar = Quote(request, "ZAR", 200);
        db.AddRange(user, customer, other, request, hiddenDraft, otherRequest, usd, zar, Quote(otherRequest, "USD", 5000));
        await db.SaveChangesAsync();
        var page = new DetailsModel(db) { Tab = "invalid" };
        await page.OnGetAsync(customer.Id);
        if (page.Customer.Name != customer.Name || page.Tab != "requests" || page.Requests.Count != 1
            || page.OpenCount != 1 || page.Quotations.Count != 2)
            throw new Exception("Customer history must use saved records, hide drafts and exclude other customers.");
        if (page.Quotations.Single(row => row.Id == usd.Id).RevisionNumber != 2
            || page.ApprovedValues.Single(value => value.CurrencyCode == "USD").Total != 100
            || page.ApprovedValues.Single(value => value.CurrencyCode == "ZAR").Total != 200
            || !page.Customer.ClientNumber.StartsWith("CUS-20261101-")
            || !page.Quotations.All(row => row.Number.StartsWith("QT-20261102-")))
            throw new Exception("History must show latest revisions, retain approved values by currency and use generated references.");
        if (await new DetailsModel(db).OnGetAsync(Guid.NewGuid()) is not NotFoundResult)
            throw new Exception("Unknown customers must return 404.");
        Console.WriteLine("PASS: database customer details, isolated history, revisions, currency totals, references and missing records.");
    }
}
