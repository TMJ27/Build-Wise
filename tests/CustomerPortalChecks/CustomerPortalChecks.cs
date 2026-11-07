using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.CustomerPortal.ViewModels;
using BuildWise.Web.Services;
using BuildWise.Web.Services.Authentication;
using BuildWise.Web.Services.CustomerPortal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal static class CustomerPortalChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static ClaimsPrincipal Customer(CustomerAccount account, string? stamp = null, string scheme = CustomerPortalAuthentication.Scheme)
        => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(CustomerPortalAuthentication.StampClaim, stamp ?? account.SecurityStamp),
            new Claim("buildwise_user_id", account.UserId.ToString()),
            new Claim("email", account.Email), new Claim("email_verified", "true")
        }, scheme));

    private static PageContext Context(ClaimsPrincipal principal) => new()
    {
        HttpContext = new DefaultHttpContext { User = principal },
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
    };

    private static CustomerRegistrationViewModel Registration(string email) => new()
    {
        Name = "Portal customer", Email = email, Address = "Harare", Phone = "0712345678",
        Password = "A long test password 123!", ConfirmPassword = "A long test password 123!"
    };

    public static async Task RunAsync()
    {
        await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var hasher = new PasswordHasher<CustomerAccount>(Options.Create(new PasswordHasherOptions { IterationCount = 600_000 }));
        var registration = new CustomerRegistrationService(db, hasher);
        Check(await registration.RegisterAsync(Registration("first@example.test")), "Valid customer registration must save.");
        Check(await registration.RegisterAsync(Registration("second@example.test")), "Another customer must have an independent account.");
        var accounts = await db.CustomerAccounts.Include(a => a.Client).Include(a => a.User).OrderBy(a => a.Email).ToListAsync();
        var first = accounts[0];
        var second = accounts[1];
        Check(first.ClientId != second.ClientId && first.UserId != second.UserId && first.Id != second.Id,
            "Each account must have distinct GUID account/client/audit identities.");
        Check(first.PasswordHash != Registration(first.Email).Password
            && hasher.VerifyHashedPassword(first, first.PasswordHash, Registration(first.Email).Password) != PasswordVerificationResult.Failed
            && hasher.VerifyHashedPassword(first, first.PasswordHash, "incorrect") == PasswordVerificationResult.Failed,
            "Passwords must be salted hashes that verify only the intended password.");
        Check(!first.User.IsActive && first.User.GroupId == null && first.Client.IsActive,
            "Customer registration must not create staff privileges.");
        Check(!await registration.RegisterAsync(Registration("FIRST@EXAMPLE.TEST")) && await db.CustomerAccounts.CountAsync() == 2,
            "Normalized duplicate emails must be rejected without extra accounts.");
        var invalid = Registration("invalid@example.test");
        invalid.Password = invalid.ConfirmPassword = "short";
        Check(!await registration.RegisterAsync(invalid), "Short passwords must fail registration.");
        invalid = Registration("invalid@example.test");
        invalid.ConfirmPassword = "Different password";
        Check(!await registration.RegisterAsync(invalid), "Mismatched confirmation must fail.");

        var currentUser = new CurrentUserService(new HttpContextAccessor(), db, new CognitoSettings());
        Check(await currentUser.GetUserIdAsync(Customer(first)) == null, "A customer cookie cannot resolve a staff user.");
        first.User.IsActive = true;
        await db.SaveChangesAsync();
        Check(await currentUser.GetUserIdAsync(Customer(first)) == null,
            "Even an accidentally activated customer audit row must remain excluded from staff authentication.");
        Check(await currentUser.EnsureUserExistsAsync(Customer(first)) == null,
            "Cognito provisioning must not promote a customer account into the bootstrap administrator.");
        var owner = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("email", "owner@example.test"), new Claim("email_verified", "true") }, "test"));
        Check(await currentUser.EnsureUserExistsAsync(owner) != null,
            "Customer registrations must not block the legitimate first staff administrator.");
        first.User.IsActive = false;
        await db.SaveChangesAsync();
        Check(!await registration.RegisterAsync(Registration("owner@example.test")), "Registration cannot reuse a staff identity.");

        // A matching customer email never grants ownership of an existing provider-created profile.
        var ownerRow = await db.Users.SingleAsync(user => user.Email == "owner@example.test");
        var existingClient = new Client
        {
            Id = Guid.NewGuid(), Name = "Existing private customer", Email = "existing@example.test", Address = "Harare",
            IsActive = true, Creator = ownerRow
        };
        db.Clients.Add(existingClient);
        await db.SaveChangesAsync();
        Check(await registration.RegisterAsync(Registration(existingClient.Email)), "A distinct account may register without claiming historical records.");
        Check((await db.CustomerAccounts.SingleAsync(a => a.Email == existingClient.Email)).ClientId != existingClient.Id,
            "Registration must never auto-link private customer records by email.");

        var portal = new CustomerPortalService(db);
        Check((await portal.GetAccountAsync(Customer(first)))?.ClientId == first.ClientId, "Customer identity must resolve its stored owner.");
        Check(await portal.GetAccountAsync(Customer(first, "wrong stamp")) == null
            && await portal.GetAccountAsync(Customer(first, scheme: CookieAuthenticationDefaults.AuthenticationScheme)) == null,
            "Revoked stamps and staff identities must not resolve a customer session.");
        first.IsActive = false;
        await db.SaveChangesAsync();
        Check(await portal.GetAccountAsync(Customer(first)) == null, "Disabled accounts cannot access the portal.");
        first.IsActive = true;
        first.Client.IsActive = false;
        await db.SaveChangesAsync();
        Check(await portal.GetAccountAsync(Customer(first)) == null, "Disabled clients cannot access the portal.");
        first.Client.IsActive = true;
        await db.SaveChangesAsync();

        var newRequest = new BuildWise.Web.Areas.CustomerPortal.Pages.Requests.CreateModel(db, portal)
        {
            PageContext = Context(Customer(first)),
            Input = new() { Title = " Paint room ", SiteAddress = " Harare ", Description = " Paint interior walls " },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData())
        };
        Check(await newRequest.OnPostAsync() is RedirectToPageResult, "Customers must be able to submit requests.");
        var request = await db.ClientRequests.SingleAsync();
        Check(request.ClientId == first.ClientId && request.CreatorId == first.UserId && request.Title == "Paint room",
            "Request ownership must come exclusively from the authenticated account.");
        var details = new BuildWise.Web.Areas.CustomerPortal.Pages.Requests.DetailsModel(portal) { PageContext = Context(Customer(second)) };
        Check(await details.OnGetAsync(request.Id) is NotFoundResult, "A customer cannot read another customer's request by GUID.");
        details.PageContext = Context(Customer(first));
        Check(await details.OnGetAsync(request.Id) is PageResult, "An owner must be able to reopen their request.");
        var dashboard = new BuildWise.Web.Areas.CustomerPortal.Pages.IndexModel(portal) { PageContext = Context(Customer(first)) };
        Check(await dashboard.OnGetAsync() is PageResult && dashboard.Dashboard.RequestCount == 1, "Dashboard must count owned requests.");
        var otherDashboard = new BuildWise.Web.Areas.CustomerPortal.Pages.IndexModel(portal) { PageContext = Context(Customer(second)) };
        await otherDashboard.OnGetAsync();
        Check(otherDashboard.Dashboard.RequestCount == 0, "Dashboard must not count another customer's requests.");

        var quote = new Quotation { Id = Guid.NewGuid(), Request = request, QuotationNumber = "QT-PORTAL", Creator = ownerRow };
        for (var number = 1; number <= 3; number++)
        {
            db.QuotationRevisions.Add(new QuotationRevision
            {
                Id = Guid.NewGuid(), Quotation = quote, RevisionNumber = number, Title = "Approved work", Creator = ownerRow,
                CompanyNameSnapshot = "BuildWise", CustomerNameSnapshot = first.Client.Name, SiteAddressSnapshot = "Harare",
                RequestDescriptionSnapshot = "Painting", StatusId = number == 3 ? (int)QuotationStatus.Draft : (int)QuotationStatus.Approved,
                ApprovalDate = number == 3 ? null : DateTime.UtcNow, Total = number * 100m, IncludesPrices = true, CurrencyCode = "USD"
            });
        }
        await db.SaveChangesAsync();
        await dashboard.OnGetAsync();
        Check(dashboard.Dashboard.Quotations.Single().RevisionNumber == 2 && dashboard.Dashboard.ApprovedQuotationCount == 1,
            "Only the latest approved revision is visible, even while a newer draft exists.");
        var quoteDetails = new BuildWise.Web.Areas.CustomerPortal.Pages.Quotations.DetailsModel(portal) { PageContext = Context(Customer(second)) };
        Check(await quoteDetails.OnGetAsync(quote.Id) is NotFoundResult, "Quotations must enforce ownership at the database query.");
        quoteDetails.PageContext = Context(Customer(first));
        Check(await quoteDetails.OnGetAsync(quote.Id) is PageResult && quoteDetails.Revision.RevisionNumber == 2,
            "Quotation details must never expose a draft.");
        var anonymousDashboard = new BuildWise.Web.Areas.CustomerPortal.Pages.IndexModel(portal) { PageContext = Context(new ClaimsPrincipal(new ClaimsIdentity())) };
        Check(await anonymousDashboard.OnGetAsync() is ChallengeResult, "Anonymous customers must be challenged.");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCognitoAuthentication(new ConfigurationBuilder().Build());
        services.AddCustomerPortal();
        using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        Check(auth.DefaultScheme == CookieAuthenticationDefaults.AuthenticationScheme, "Adding the portal must preserve the staff default scheme.");
        var cookie = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CustomerPortalAuthentication.Scheme);
        Check(cookie.Cookie.Name == "BuildWise.Customer" && cookie.LoginPath == "/CustomerPortal/Account/Login"
            && cookie.Cookie.SecurePolicy == CookieSecurePolicy.Always, "Customer cookie must be separate and HTTPS-only.");
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(CustomerPortalAuthentication.Policy);
        Check(policy!.AuthenticationSchemes.SequenceEqual(new[] { CustomerPortalAuthentication.Scheme }), "Portal policy must authenticate only the customer scheme.");

        // Schema consistency and migration scripting require no database connection.
        await using var schema = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=BuildWise_SchemaOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        Check(!schema.Database.HasPendingModelChanges(), "Migration snapshot must match the current model.");
        var migrations = schema.Database.GetMigrations().ToArray();
        Check(migrations.Last().EndsWith("CustomerPortalAccounts"), "New account migration must be discoverable in the Data assembly.");
        var script = schema.GetService<IMigrator>().GenerateScript(migrations[^2], migrations[^1], MigrationsSqlGenerationOptions.Idempotent);
        Check(script.Contains("CREATE TABLE [CustomerAccount]") && script.Contains("UNIQUE INDEX [IX_CustomerAccount_NormalizedEmail]")
            && !script.Contains("DROP TABLE"), "Migration must add account ownership/unique email indexes without deleting existing tables.");
        Console.WriteLine("PASS: registration/password hashes, customer isolation, staff separation/bootstrap, request ownership, approved-only quotations, session revocation, policy/cookies and migration consistency.");
    }

    private sealed class MemoryTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
