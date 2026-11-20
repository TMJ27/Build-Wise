using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.CustomerPortal.Pages.Quotations;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class QuotationResponseChecks
{
    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var staff = new User { Id = Guid.NewGuid(), Email = "staff@example.test", PasswordHash = "test" };
        CustomerAccount Account(string name)
        {
            var client = new Client { Id = Guid.NewGuid(), Name = name, Email = $"{name}@example.test", Address = "Site", Creator = staff, IsActive = true };
            return new CustomerAccount
            {
                Id = Guid.NewGuid(), Client = client, ClientId = client.Id, User = staff, UserId = staff.Id,
                Email = client.Email, NormalizedEmail = client.Email.ToUpperInvariant(), PasswordHash = "test", SecurityStamp = name, IsActive = true
            };
        }
        var account = Account("customer");
        var other = Account("other");
        // Each customer account has its own audit user in the real schema.
        other.User = new User { Id = Guid.NewGuid(), Email = other.Email, PasswordHash = "test" };
        other.UserId = other.User.Id;
        var request = new ClientRequest { Id = Guid.NewGuid(), Client = account.Client, Creator = staff, Title = "Work", SiteAddress = "Site" };
        var quotation = new Quotation { Id = Guid.NewGuid(), Request = request, Creator = staff, QuotationNumber = "legacy" };
        QuotationRevision Revision(int number, QuotationStatus status = QuotationStatus.Approved) => new()
        {
            Id = Guid.NewGuid(), Quotation = quotation, Creator = staff, RevisionNumber = number, Title = "Reviewed work",
            CompanyNameSnapshot = "Company", CustomerNameSnapshot = account.Client.Name, SiteAddressSnapshot = "Site",
            RequestDescriptionSnapshot = "Work", StatusId = (int)status, CreationDate = DateTime.Now,
            ApprovalDate = status == QuotationStatus.Approved ? DateTime.Now : null,
            ValidUntil = DateTime.Now.AddDays(7)
        };
        var first = Revision(1);
        var draft = Revision(2, QuotationStatus.Draft);
        db.AddRange(staff, account, other, request, quotation, first, draft);
        await db.SaveChangesAsync();
        var portal = new CustomerPortalService(db);
        DetailsModel Page(CustomerAccount customer)
        {
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new Claim(CustomerPortalAuthentication.StampClaim, customer.SecurityStamp)
            }, CustomerPortalAuthentication.Scheme)) };
            return new DetailsModel(db, portal)
            {
                PageContext = new PageContext
                {
                    HttpContext = http,
                    ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                },
                TempData = new TempDataDictionary(http, new MemoryTempData())
            };
        }
        Check(await Page(other).OnGetAsync(quotation.Id) is NotFoundResult, "Other customers cannot view the quotation.");
        var forged = Page(other);
        forged.Input.RevisionId = first.Id;
        Check(await forged.OnPostAcceptAsync(quotation.Id) is NotFoundResult, "Other customers cannot respond.");
        var form = Page(account);
        await form.OnGetAsync(quotation.Id);
        Check(form.Revision.Id == first.Id, "A newer draft must not replace the visible approved revision.");
        await form.OnPostAcceptAsync(quotation.Id);
        Check(!form.ModelState.IsValid && !await db.QuotationResponses.AnyAsync(), "Confirmation is required.");
        form = Page(account);
        await form.OnGetAsync(quotation.Id);
        form.Input.Confirm = true;
        Check(await form.OnPostAcceptAsync(quotation.Id) is RedirectToPageResult, "Confirmed acceptance must save.");
        var saved = await db.QuotationResponses.SingleAsync();
        Check(saved.StatusId == (int)QuotationResponseStatus.Accepted && saved.QuotationRevisionId == first.Id
            && saved.CustomerAccountId == account.Id && saved.CustomerEmailSnapshot == account.Email
            && first.StatusId == (int)QuotationStatus.Approved, "Acceptance must retain exact revision/customer/audit and internal approval.");
        var staffDetails = new BuildWise.Web.Areas.Quotations.Pages.DetailsModel(db, null!);
        await staffDetails.OnGetAsync(quotation.Id, first.Id);
        Check(staffDetails.CustomerResponse?.Id == saved.Id,
            "Staff details must display the response belonging to the selected revision.");
        var dashboard = new BuildWise.Web.Areas.CustomerPortal.Pages.IndexModel(db, portal) { PageContext = Page(account).PageContext };
        await dashboard.OnGetAsync();
        Check(dashboard.Dashboard.Quotations.Single().ResponseStatus == QuotationResponseStatus.Accepted,
            "Customer quotation table must show Accepted for the visible revision.");
        var repeat = Page(account);
        await repeat.OnGetAsync(quotation.Id);
        repeat.Input.Confirm = true;
        repeat.Input.DeclineReason = "Changed my mind";
        await repeat.OnPostDeclineAsync(quotation.Id);
        Check(!repeat.ModelState.IsValid && await db.QuotationResponses.CountAsync() == 1, "A response cannot be overwritten.");
        var stale = Page(account);
        await stale.OnGetAsync(quotation.Id);
        stale.Input.Confirm = true;
        draft.StatusId = (int)QuotationStatus.Approved;
        draft.ApprovalDate = DateTime.Now;
        await db.SaveChangesAsync();
        await stale.OnPostAcceptAsync(quotation.Id);
        Check(!stale.ModelState.IsValid && await db.QuotationResponses.CountAsync() == 1, "Older forms cannot accept a newly approved revision.");
        form = Page(account);
        await form.OnGetAsync(quotation.Id);
        Check(form.CustomerResponse is null, "A new revision must require its own customer response.");
        await dashboard.OnGetAsync();
        Check(dashboard.Dashboard.Quotations.Single().ResponseStatus is null,
            "A newly approved revision must await its own response in the customer table.");
        await staffDetails.OnGetAsync(quotation.Id, draft.Id);
        Check(staffDetails.CustomerResponse is null, "A previous revision's response must not be shown as the new revision's response.");
        form.Input.Confirm = true;
        await form.OnPostDeclineAsync(quotation.Id);
        Check(!form.ModelState.IsValid && await db.QuotationResponses.CountAsync() == 1, "Declining requires a reason.");
        draft.ValidUntil = DateTime.Now.AddDays(-2);
        await db.SaveChangesAsync();
        form = Page(account);
        await form.OnGetAsync(quotation.Id);
        form.Input.Confirm = true;
        await form.OnPostAcceptAsync(quotation.Id);
        Check(!form.ModelState.IsValid && await db.QuotationResponses.CountAsync() == 1, "Expired quotations cannot be accepted.");
        form = Page(account);
        await form.OnGetAsync(quotation.Id);
        form.Input.Confirm = true;
        form.Input.DeclineReason = "  Budget changed  ";
        Check(await form.OnPostDeclineAsync(quotation.Id) is RedirectToPageResult, "A reasoned decline must save.");
        Check((await db.QuotationResponses.SingleAsync(response => response.QuotationRevisionId == draft.Id)).Reason == "Budget changed",
            "Decline reason must be trimmed and retained on the correct revision.");
        await dashboard.OnGetAsync();
        Check(dashboard.Dashboard.Quotations.Single().ResponseStatus == QuotationResponseStatus.Declined,
            "Customer quotation table must show Declined after the response is saved.");
        Console.WriteLine("PASS: customer ownership, confirmation, revision-bound acceptance/decline, audit, duplicate/stale response guards and expiry.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
