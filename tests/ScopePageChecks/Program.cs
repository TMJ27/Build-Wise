using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Net;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Lib.Users;
using BuildWise.Web.Services.Scopes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".."));
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(BuildWise.Web.Areas.Scopes.Pages.DetailsModel).Assembly.GetName().Name,
    ContentRootPath = root,
    WebRootPath = Path.Combine(root, "wwwroot"),
    EnvironmentName = "Development"
});
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Logging.ClearProviders();
builder.Services.AddRazorPages();
builder.Services.AddRateLimiter(options => options.AddPolicy("GeminiScopeDraft", _ =>
    System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("test")));
builder.Services.AddRateLimiter(options => options.AddPolicy("CustomerPasswordRecovery", _ =>
    System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("test")));
builder.Services.AddScoped<BuildWise.Web.Services.CustomerPortal.CustomerPasswordRecoveryService>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<CustomerAccount>, Microsoft.AspNetCore.Identity.PasswordHasher<CustomerAccount>>();
builder.Services.AddSingleton<BuildWise.Web.Services.Email.IEmailSender, NoRecoveryEmail>();
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
builder.Services.AddDbContext<BuildWiseDbContext>(options => options.UseInMemoryDatabase("scope-page-check"));
var userId = Guid.NewGuid();
builder.Services.AddSingleton<ICurrentUserService>(new CurrentUser(userId));
builder.Services.AddHttpClient<GeminiScopeDraftService>();
var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.MapRazorPages();
var requestId = Guid.NewGuid();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
    var user = new User { Id = userId, Email = "staff@example.test", PasswordHash = "test" };
    var client = new Client { Id = Guid.NewGuid(), Name = "Test client", Email = "test@example.test", Address = "Test site", Creator = user };
    db.AddRange(user, client, new ClientRequest { Id = requestId, Client = client, Creator = user, Title = "House tubing", SiteAddress = "Test site", StatusId = (int)ClientRequestStatus.Open });
    await db.SaveChangesAsync();
}
await app.StartAsync();
try
{
    var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    using var client = new HttpClient { BaseAddress = new Uri(url) };
    var recoveryHtml = await client.GetStringAsync("/CustomerPortal/Account/ForgotPassword");
    if (!recoveryHtml.Contains("Send reset link") || !recoveryHtml.Contains("Input.Email"))
        throw new Exception("Forgot password page must render its email form anonymously.");
    var invalidReset = await client.GetStringAsync("/CustomerPortal/Account/ResetPassword?token=invalid");
    if (!invalidReset.Contains("invalid, expired or already used") || invalidReset.Contains("name=\"Input.Password\""))
        throw new Exception("Invalid reset links must render a clear error without a password form.");
    string resetToken;
    using (var recoveryScope = app.Services.CreateScope())
    {
        var db = recoveryScope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
        var customer = await db.Clients.SingleAsync();
        customer.IsActive = true;
        var account = new CustomerAccount
        {
            Id = Guid.NewGuid(), ClientId = customer.Id, UserId = userId, Email = "recovery@example.test",
            NormalizedEmail = "RECOVERY@EXAMPLE.TEST", IsActive = true, SecurityStamp = "test-recovery-stamp", PasswordHash = "test-only"
        };
        db.CustomerAccounts.Add(account);
        await db.SaveChangesAsync();
        resetToken = recoveryScope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("BuildWise.CustomerPasswordReset.v1").Protect(System.Text.Json.JsonSerializer.Serialize(new
            { AccountId = account.Id, Stamp = account.SecurityStamp, Email = account.NormalizedEmail, Expires = DateTime.Now.AddMinutes(30) }));
    }
    var resetResponse = await client.GetAsync("/CustomerPortal/Account/ResetPassword?token=" + Uri.EscapeDataString(resetToken));
    var resetHtml = await resetResponse.Content.ReadAsStringAsync();
    if (!resetResponse.IsSuccessStatusCode || !resetHtml.Contains("name=\"Input.Password\"")
        || !resetHtml.Contains("name=\"Input.ConfirmPassword\"") || !resetHtml.Contains("name=\"Input.Token\"")
        || resetResponse.Headers.GetValues("Referrer-Policy").Single() != "no-referrer")
        throw new Exception("Valid recovery page must render password fields and prevent referrer leakage.");
    Console.WriteLine("PASS: anonymous recovery form and invalid reset-link page render.");
    var response = await client.GetAsync($"/Scopes/Details/{requestId}");
    var html = await response.Content.ReadAsStringAsync();
    await File.WriteAllTextAsync(Path.Combine(root, "obj/scope-page-check.html"), html);
    if (!response.IsSuccessStatusCode) throw new Exception("Scope page failed: " + response.StatusCode);
    if (!html.Contains("<h1>Scope of work</h1>") || !html.Contains("Suggest with AI")) throw new Exception("Scope labels were not rendered.");
    var link = Regex.Matches(html, "<a\\b[^>]*href=\"([^\"]+)\"[^>]*>[\\s\\S]*?</a>")
        .Cast<Match>().Single(match => match.Value.Contains("<span>Add service</span>"));
    var target = WebUtility.HtmlDecode(link.Groups[1].Value);
    if (!target.Contains("/Scopes/Items/Upsert") || !target.Contains(requestId.ToString())) throw new Exception("Add service has the wrong route: " + target);
    var formResponse = await client.GetAsync(target);
    var form = await formResponse.Content.ReadAsStringAsync();
    if (!formResponse.IsSuccessStatusCode || !form.Contains("Add service to scope")) throw new Exception("Add service form could not be opened.");
    if (!Regex.IsMatch(form, "<option selected=\"selected\" value=\"" + requestId + "\"")) throw new Exception("The request must be preselected on the add form.");
    var itemId = Guid.NewGuid();
    using (var serviceScope = app.Services.CreateScope())
    {
        var db = serviceScope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
        var request = await db.ClientRequests.SingleAsync();
        request.ReferenceNumber = "REQ-20261109-0001";
        request.CreationDate = new DateTime(2026, 11, 9);
        var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Electrical" };
        var service = new Service { Id = Guid.NewGuid(), Name = "Tubing", Category = category, CreatorId = userId };
        db.ScopeItems.Add(new ScopeItem { Id = itemId, Request = request, Service = service, Title = "House tubing", ServiceCodeSnapshot = "SRV-0001", ReviewedByUserId = userId });
        await db.SaveChangesAsync();
    }
    var details = await client.GetStringAsync($"/Scopes/Items/Details/{itemId}?requestId={requestId}");
    if (!details.Contains("SCP-20261109-0001") || !details.Contains("REQ-20261109-0001")) throw new Exception("Scope details must display generated references.");
    if (details.Contains($">{itemId}<") || details.Contains($">{requestId}<")) throw new Exception("GUIDs must not appear as display references.");
    var clausesPage = await client.GetStringAsync($"/Scopes/Clauses/{itemId}");
    foreach (var sourceHtml in new[] { details, clausesPage })
    {
        var addClause = Regex.Matches(sourceHtml, "<a\\b[^>]*href=\"([^\"]+)\"[^>]*>[\\s\\S]*?</a>")
            .Cast<Match>().Single(match => match.Value.Contains("<span>Add clause</span>"));
        var clauseTarget = WebUtility.HtmlDecode(addClause.Groups[1].Value);
        if (!clauseTarget.Contains("/Scopes/Clauses/Upsert") || !clauseTarget.Contains(itemId.ToString()))
            throw new Exception("Add clause must target the separate create form for this scope item.");
        var clauseResponse = await client.GetAsync(clauseTarget);
        var clauseForm = await clauseResponse.Content.ReadAsStringAsync();
        if (!clauseResponse.IsSuccessStatusCode || !clauseForm.Contains("Add scope clause")
            || !Regex.IsMatch(clauseForm, "name=\"Input.ScopeItemId\"[^>]*value=\"" + itemId + "\""))
            throw new Exception("Add clause form must open with the correct parent scope item.");
    }
    Console.WriteLine("PASS: Add clause links from both details and clause list open the separate form with the correct scope item.");
    Console.WriteLine("PASS: Scope of work labels, Add service href, target form GET and selected request.");
    Console.WriteLine("PASS: generated scope item number and saved request reference replace visible GUIDs.");
    var quoteId = Guid.NewGuid();
    using (var serviceScope = app.Services.CreateScope())
    {
        var db = serviceScope.ServiceProvider.GetRequiredService<BuildWiseDbContext>();
        var quotation = new Quotation { Id = quoteId, RequestId = requestId, CreatorId = userId, QuotationNumber = "Saved quote" };
        db.QuotationRevisions.Add(new QuotationRevision
        {
            Id = Guid.NewGuid(), Quotation = quotation, RevisionNumber = 1, Title = "Saved quotation title",
            CompanyNameSnapshot = "Snapshot company", CustomerNameSnapshot = "Snapshot customer",
            SiteAddressSnapshot = "Snapshot site", RequestDescriptionSnapshot = "Saved request description",
            CreatorId = userId, StatusId = (int)QuotationStatus.Draft, CreationDate = new DateTime(2026, 11, 10)
        });
        await db.SaveChangesAsync();
    }
    var quotationDetails = await client.GetStringAsync($"/Quotations/Details/{quoteId}");
    if (!quotationDetails.Contains("Preview quotation") || !quotationDetails.Contains("Approve quotation")
        || !quotationDetails.Contains("QT-20261110-0001")) throw new Exception("Quotation next-step actions were not rendered.");
    var quotationPreview = await client.GetStringAsync($"/Quotations/Preview/{quoteId}");
    if (!quotationPreview.Contains("Snapshot company") || !quotationPreview.Contains("Snapshot customer")
        || !quotationPreview.Contains("print-quotation")) throw new Exception("Preview must render saved snapshot data and print action.");
    Console.WriteLine("PASS: database quotation Details and Preview render with approval, print and saved snapshot data.");
    if (args.Contains("--review-recovery"))
    {
        Console.WriteLine("RECOVERY_REVIEW_URL=" + url);
        await app.WaitForShutdownAsync();
    }
}
finally { await app.StopAsync(); }

sealed class CurrentUser(Guid id) : ICurrentUserService
{
    public Task<Guid?> GetUserIdAsync() => Task.FromResult<Guid?>(id);
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
}

sealed class NoRecoveryEmail : BuildWise.Web.Services.Email.IEmailSender
{
    public Task<string> SendAsync(string recipient, string subject, string bodyHtml, Guid creatorId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("No live email is allowed from the page review fixture.");
    public Task<string> SendQuotationAsync(string recipient, string subject, string bodyHtml, Guid creatorId,
        Guid revisionId, byte[] pdf, string fileName, Guid sendId, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("No live email is allowed from the page review fixture.");
}
