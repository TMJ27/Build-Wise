using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Quotations.Pages;
using BuildWise.Web.Areas.Quotations.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

internal static class QuotationUpsertChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Validate(UpsertModel form)
    {
        static void Add(UpsertModel form, object input, string prefix)
        {
            var errors = new List<ValidationResult>();
            Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
            foreach (var error in errors)
                form.ModelState.AddModelError(prefix + error.MemberNames.FirstOrDefault(), error.ErrorMessage!);
        }
        Add(form, form.Input, "Input.");
        for (var i = 0; i < form.Input.Items.Count; i++) Add(form, form.Input.Items[i], $"Input.Items[{i}].");
    }

    public static async Task RunAsync()
    {
        var options = new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new BuildWiseDbContext(options);
        var user = new User { Id = Guid.NewGuid(), Email = "quote@example.test", PasswordHash = "test" };
        var company = new Company
        {
            Id = Guid.NewGuid(), Name = "BuildWise", RegistrationNumber = "REG", TaxNumber = "TAX",
            Email = "business@example.test", Phone = "123", Address = "Harare", DefaultCurrency = "USD",
            DefaultQuotationTerms = "Payment on completion", DefaultValidityDays = 14
        };
        var client = new Client { Id = Guid.NewGuid(), Name = "Original customer", Email = "customer@example.test", Address = "Harare", Creator = user };
        var request = new ClientRequest
        {
            Id = Guid.NewGuid(), Title = "Painting", SiteAddress = "Site address", Description = "Paint walls",
            Client = client, Creator = user, StatusId = (int)ClientRequestStatus.Open
        };
        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Painting", IsActive = true };
        var service = new Service { Id = Guid.NewGuid(), Name = "Painting", Category = category, Creator = user };
        var scope = new ScopeItem
        {
            Id = Guid.NewGuid(), Request = request, Service = service, ServiceCodeSnapshot = "PAINT-1",
            Title = "Paint walls", Quantity = 2, UnitCodeSnapshot = "rooms", ReviewedByUser = user,
            RequireAssessment = true, AssessmentNotesJson = "Access required"
        };
        db.AddRange(company, user, client, request, category, service, scope,
            new Currency { Id = Guid.NewGuid(), Code = "USD", Name = "US Dollar", Symbol = "$", Creator = user });
        await db.SaveChangesAsync();
        UpsertModel Form(Guid? userId = null) => new(db, new CurrentUser(userId ?? user.Id))
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };

        var form = Form();
        Check(await form.OnGetAsync(null, request.Id) is PageResult, "Create must load a database request.");
        Check(form.Input.Items.Single().ScopeItemId == scope.Id && form.Input.CurrencyCode == "USD"
            && form.Input.Terms == company.DefaultQuotationTerms, "Scope and company defaults must load.");
        form.Input.Items[0].LineAmount = 125.55m;
        form.Input.TaxAmount = 10m;
        Validate(form);
        Check(await form.OnPostAsync(null) is RedirectToPageResult, "Create must save successfully.");
        var quote = await db.Quotations.SingleAsync();
        var revision = await db.QuotationRevisions.Include(r => r.QuotationItems).SingleAsync();
        Check(revision.Total == 135.55m && revision.Subtotal == 125.55m && revision.TaxAmount == 10m,
            "Total must sum full line amounts plus tax without multiplying quantity again.");
        Check(revision.RevisionNumber == 1 && revision.CreatorId == user.Id && quote.CreatorId == user.Id,
            "Creation must save the first revision and creator.");
        Check(revision.CustomerNameSnapshot == client.Name && revision.CompanyNameSnapshot == company.Name,
            "Company and customer snapshots must be saved.");
        Check(revision.QuotationItems.Single().ServiceId == service.Id && revision.QuotationItems.Single().RequestId == request.Id,
            "Items must retain real scope, service and request relationships.");

        var edit = Form();
        await edit.OnGetAsync(quote.Id, null);
        var stale = Form();
        await stale.OnGetAsync(quote.Id, null);
        edit.Input.Items[0].LineAmount = 200m;
        Validate(edit);
        Check(await edit.OnPostAsync(quote.Id) is RedirectToPageResult && await db.QuotationRevisions.CountAsync() == 1,
            "Editing a draft must update the same revision.");
        Check(revision.Total == 210m && await db.QuotationItems.CountAsync() == 1, "Draft edit must replace items and recalculate totals.");
        Check(await stale.OnPostAsync(quote.Id) is PageResult && !stale.ModelState.IsValid,
            "A stale form must not overwrite a newer save.");

        revision.StatusId = (int)QuotationStatus.Approved;
        revision.ApprovedByUserId = user.Id;
        revision.ApprovalDate = DateTime.UtcNow;
        revision.ApprovedDocumentJson = "approved document";
        await db.SaveChangesAsync();
        client.Name = "Changed customer";
        company.Name = "Changed company";
        await db.SaveChangesAsync();
        var revise = Form();
        await revise.OnGetAsync(quote.Id, null);
        Check(revise.CreatesRevision && revise.RevisionNumber == 2, "Approved quotation must advertise a new revision.");
        revise.Input.Items[0].LineAmount = 300m;
        Validate(revise);
        Check(await revise.OnPostAsync(quote.Id) is RedirectToPageResult, "Approved quotation must save a new draft revision.");
        var next = await db.QuotationRevisions.Include(r => r.QuotationItems).SingleAsync(r => r.RevisionNumber == 2);
        Check(revision.Total == 210m && revision.StatusId == (int)QuotationStatus.Approved && revision.QuotationItems.Single().LineAmount == 200m,
            "Approved totals and items must remain unchanged.");
        Check(next.Total == 310m && next.StatusId == (int)QuotationStatus.Draft && next.ApprovalDate == null
            && next.ApprovedByUserId == null && next.ApprovedDocumentJson == null, "New revision must clear approval and calculate new totals.");
        Check(next.CustomerNameSnapshot == "Original customer" && next.CompanyNameSnapshot == "BuildWise",
            "Revisions must preserve the original document snapshots.");

        var invalid = Form();
        await invalid.OnGetAsync(quote.Id, null);
        invalid.Input.Items[0].ScopeItemId = Guid.NewGuid();
        Check(await invalid.OnPostAsync(quote.Id) is PageResult && !invalid.ModelState.IsValid,
            "Foreign scope items must be rejected.");
        var unpriced = Form();
        await unpriced.OnGetAsync(quote.Id, null);
        unpriced.Input.IncludesPrices = false;
        unpriced.Input.Items[0].LineAmount = null;
        Check(await unpriced.OnPostAsync(quote.Id) is RedirectToPageResult && next.Total == null
            && next.CurrencyCode == null && next.QuotationItems.Single().LineAmount == null,
            "Scope-only quotes must clear prices and totals.");
        var missing = Form();
        Check(await missing.OnGetAsync(Guid.NewGuid(), null) is NotFoundResult, "Missing quotation must return 404.");
        var tampered = Form();
        Check(await tampered.OnPostAsync(quote.Id) is BadRequestResult, "Route and form IDs must match.");
        var unauthorized = new UpsertModel(db, new CurrentUser(null));
        Check(await unauthorized.OnPostAsync(null) is UnauthorizedResult, "Saving requires a current user.");
        Console.WriteLine("PASS: quotation create/edit, totals, snapshots, revision history, stale forms, scope validation, unpriced quotes and identity checks.");
    }

    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class CurrentUser(Guid? id) : ICurrentUserService
    {
        public Task<Guid?> GetUserIdAsync() => Task.FromResult(id);
        public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
        public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    }
}
