using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Rights;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Configuration.Pages.UserGroups;
using BuildWise.Web.Areas.Configuration.ViewModels.UserGroups;
using BuildWise.Web.Services;
using BuildWise.Web.Services.Authentication;
using BuildWise.Web.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static ClaimsPrincipal Account(string email, bool verified = true)
{
    return new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim("email", email),
        new Claim("email_verified", verified.ToString().ToLowerInvariant())
    }, "test"));
}

static UpsertModel Form(BuildWiseDbContext db, ICurrentUserService currentUser, DefaultHttpContext context,
    string name, int rights, Guid? id = null)
{
    return new UpsertModel(db, currentUser)
    {
        PageContext = new PageContext { HttpContext = context },
        TempData = new TempDataDictionary(context, new MemoryTempData()),
        Input = new UserGroupUpsertViewModel { Id = id, Name = name, RightsId = rights }
    };
}

await using var db = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var creator = new User
{
    Id = Guid.NewGuid(), Name = "Test", Surname = "Creator", Email = "creator@example.test",
    IsActive = true, PasswordHash = "unused", CreationDate = DateTime.UtcNow
};
db.Users.Add(creator);
await db.SaveChangesAsync();
var http = new DefaultHttpContext { User = Account(creator.Email) };
var accessor = new HttpContextAccessor { HttpContext = http };
var currentUser = new CurrentUserService(accessor, db, new CognitoSettings());
Check(await currentUser.GetUserIdAsync() == creator.Id, "Verified email must resolve the local user ID.");
Check(await currentUser.GetUserIdAsync(Account(creator.Email, false)) is null, "Unverified email must not identify a creator.");
Check(await currentUser.GetUserIdAsync(new ClaimsPrincipal(new ClaimsIdentity())) is null, "Anonymous requests cannot identify a creator.");
creator.IsActive = false;
await db.SaveChangesAsync();
Check(await currentUser.GetUserIdAsync() is null, "Inactive accounts must be refused.");
creator.IsActive = true;
await db.SaveChangesAsync();

var rights = (int)(AccessRights.ViewUsers | AccessRights.CreateUsers);
var create = Form(db, currentUser, http, "Office", rights);
Check(await create.OnGetAsync(null) is PageResult, "Create GET should show a blank form.");
var beforeCreate = DateTime.UtcNow;
Check(await create.OnPostAsync(null) is RedirectToPageResult, "Valid creation should redirect to details.");
var saved = await db.UserGroups.SingleAsync();
Check(saved.Id != Guid.Empty && saved.CreatorId == creator.Id, "Creation must store the actual local creator ID.");
Check(saved.CreationDate >= beforeCreate && saved.CreationDate <= DateTime.UtcNow, "Creation date must be assigned on the server.");
Check(saved.RightsId == rights, "Selected flags must persist unchanged.");
Check((string?)create.TempData["SuccessMessage"] == "User group created successfully.", "Creation message must be correct.");
var originalDate = saved.CreationDate;

var editor = new User
{
    Id = Guid.NewGuid(), Name = "Another", Surname = "Editor", Email = "editor@example.test",
    IsActive = true, PasswordHash = "unused", CreationDate = DateTime.UtcNow
};
db.Users.Add(editor);
await db.SaveChangesAsync();
http.User = Account(editor.Email);
var edit = Form(db, currentUser, http, "Office updated", (int)AccessRights.ViewUsers, saved.Id);
Check(await edit.OnPostAsync(saved.Id) is RedirectToPageResult, "Existing group should update.");
Check(saved.CreatorId == creator.Id && saved.CreationDate == originalDate, "Editing must preserve creator and creation date.");
Check((string?)edit.TempData["SuccessMessage"] == "User group updated successfully.", "Update message must be correct.");
var missingId = Guid.NewGuid();
var missing = Form(db, currentUser, http, "Missing", 0, missingId);
Check(await missing.OnPostAsync(missingId) is NotFoundResult, "A missing edit target must not create a group.");
var mismatch = Form(db, currentUser, http, "Mismatch", 0, Guid.NewGuid());
Check(await mismatch.OnPostAsync(saved.Id) is BadRequestResult, "Route and posted IDs must agree.");
var invalidRights = Form(db, currentUser, http, "Invalid", 1 << 30);
Check(await invalidRights.OnPostAsync(null) is PageResult && !invalidRights.ModelState.IsValid,
    "Undefined flags must be rejected with a visible validation error.");
http.User = new ClaimsPrincipal(new ClaimsIdentity());
var noCreator = Form(db, currentUser, http, "Anonymous", 0);
Check(await noCreator.OnPostAsync(null) is PageResult && !noCreator.ModelState.IsValid,
    "Creation without a valid creator must return validation errors.");
Check(await db.UserGroups.CountAsync() == 1, "Failed requests must not insert records.");

creator.GroupId = saved.Id;
await db.SaveChangesAsync();
var details = new DetailsModel(db);
Check(await details.OnGetAsync(saved.Id) is PageResult, "Details should load an existing group.");
Check(details.GroupDetails.CreatorName == "Test Creator", "Details must resolve the actual creator name.");
Check(details.GroupDetails.UserCount == 1 && details.GroupDetails.Members.Single().Id == creator.Id,
    "Details must load real group membership.");
Check(details.GroupDetails.AssignedRights.Single().Value == AccessRights.ViewUsers, "Details must show only assigned rights.");
Check(await details.OnGetAsync(Guid.NewGuid()) is NotFoundResult, "Missing details must return 404.");

var settings = new Dictionary<string, string?>
{
    ["Cognito:Authority"] = "https://cognito-idp.test.amazonaws.com/test_pool",
    ["Cognito:ClientId"] = "test-client",
    ["Cognito:Domain"] = "https://login.example.test",
    ["Cognito:LogoutUri"] = "https://localhost:7213/"
};
var services = new ServiceCollection();
services.AddLogging();
services.AddDataProtection().UseEphemeralDataProtectionProvider();
services.AddSingleton<ICurrentUserService>(currentUser);
services.AddCognitoAuthentication(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
using var provider = services.BuildServiceProvider();
var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.FallbackPolicy;
Check(policy is not null && policy.Requirements.Count > 0, "Workspace endpoints need a default authorization policy.");
var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);
Check(oidc.CallbackPath == "/signin-oidc" && oidc.UsePkce && oidc.ResponseType == "code", "OIDC callback and code flow must be configured.");
Check(oidc.Scope.Contains("openid") && oidc.Scope.Contains("email") && oidc.Scope.Contains("profile"), "Cognito scopes must match the application.");
var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));
var ticketHttp = new DefaultHttpContext { RequestServices = provider };
var principal = Account(creator.Email);
var ticket = new TicketReceivedContext(ticketHttp, scheme, oidc,
    new AuthenticationTicket(principal, new AuthenticationProperties(), scheme.Name));
await oidc.Events.OnTicketReceived(ticket);
Check(principal.FindFirst("buildwise_user_id")?.Value == creator.Id.ToString(), "Successful login must stamp the local user ID.");
var denied = new TicketReceivedContext(ticketHttp, scheme, oidc,
    new AuthenticationTicket(Account("unknown@example.test"), new AuthenticationProperties(), scheme.Name));
await oidc.Events.OnTicketReceived(denied);
Check(ticketHttp.Response.Headers.Location == "/Identity/Account/AccessDenied", "Unknown accounts must not receive an application session.");
var pending = await db.Users.SingleAsync(user => user.Email == "unknown@example.test");
Check(!pending.IsActive && pending.GroupId is null && pending.IsEmailConfirmed,
    "New verified accounts must be provisioned pending approval, without a group.");
Check(!string.IsNullOrWhiteSpace(pending.PasswordHash), "The required password column must receive an unusable random password hash.");
await currentUser.EnsureUserExistsAsync(Account("UNKNOWN@example.test"));
Check(await db.Users.CountAsync(user => user.Email.ToLower() == "unknown@example.test") == 1,
    "Repeated or differently cased sign-ins must not duplicate accounts.");
var userCountBeforeUnverified = await db.Users.CountAsync();
await currentUser.EnsureUserExistsAsync(Account("unverified@example.test", false));
Check(await db.Users.CountAsync() == userCountBeforeUnverified, "Unverified email must not create a local user.");
var oldCreator = creator.Id;
await currentUser.EnsureUserExistsAsync(Account(creator.Email));
Check(creator.Id == oldCreator && creator.GroupId == saved.Id && creator.LastLoginDate.HasValue,
    "Existing active users retain identity and group and receive a sign-in timestamp.");
var lastLogin = creator.LastLoginDate;
http.User = Account(creator.Email);
await currentUser.EnsureUserExistsAsync();
Check(creator.LastLoginDate == lastLogin, "Ordinary middleware requests must not rewrite the sign-in timestamp.");

await using var bootstrapDb = new BuildWiseDbContext(new DbContextOptionsBuilder<BuildWiseDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
var bootstrapSettings = new CognitoSettings { BootstrapAdminEmail = "admin@example.test" };
var bootstrapService = new CurrentUserService(new HttpContextAccessor(), bootstrapDb, bootstrapSettings);
Check(await bootstrapService.EnsureUserExistsAsync(Account("visitor@example.test")) is null,
    "An arbitrary first visitor must not receive administrator access.");
var bootstrapId = await bootstrapService.EnsureUserExistsAsync(Account("ADMIN@example.test"));
Check(bootstrapId.HasValue, "The configured verified first administrator should be activated.");
var bootstrapUser = await bootstrapDb.Users.Include(user => user.Group).SingleAsync(user => user.Id == bootstrapId);
Check(bootstrapUser.IsActive && bootstrapUser.Group?.RightsId == (int)AccessRights.All
    && bootstrapUser.Group.CreatorId == bootstrapUser.Id, "Bootstrap must create the rights group with a real creator FK.");
Check(await bootstrapService.EnsureUserExistsAsync(Account("admin@example.test")) == bootstrapId,
    "Repeated administrator login must reuse the bootstrap user.");
Check(await bootstrapDb.UserGroups.CountAsync() == 1, "Repeated bootstrap login must not duplicate administrator groups.");
bootstrapUser.IsActive = false;
await bootstrapDb.SaveChangesAsync();
Check(await bootstrapService.EnsureUserExistsAsync(Account("admin@example.test")) is null && !bootstrapUser.IsActive,
    "Provisioning must never reactivate an explicitly disabled user.");
bootstrapUser.IsActive = true;
await bootstrapDb.SaveChangesAsync();
bootstrapSettings.BootstrapAdminEmail = "second-admin@example.test";
Check(await bootstrapService.EnsureUserExistsAsync(Account("second-admin@example.test")) is null,
    "Bootstrap must not grant administrator access after an active account exists.");

var nextCalls = 0;
var middleware = new UserProvisioningMiddleware(_ => { nextCalls++; return Task.CompletedTask; },
    NullLogger<UserProvisioningMiddleware>.Instance);
var middlewareUser = new StubCurrentUser { Id = creator.Id };
await middleware.InvokeAsync(new DefaultHttpContext(), middlewareUser);
Check(nextCalls == 1 && middlewareUser.Calls == 0, "Anonymous requests should bypass provisioning.");
var workspaceHttp = new DefaultHttpContext { User = Account(creator.Email), RequestServices = provider };
await middleware.InvokeAsync(workspaceHttp, middlewareUser);
Check(nextCalls == 2 && middlewareUser.Calls == 1, "Authenticated workspace requests should provision before continuing.");
workspaceHttp.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
    new EndpointMetadataCollection(new AllowAnonymousAttribute()), "Public page"));
await middleware.InvokeAsync(workspaceHttp, middlewareUser);
Check(nextCalls == 3 && middlewareUser.Calls == 1, "Public endpoints should not run provisioning.");
workspaceHttp.SetEndpoint(null);
middlewareUser.Id = null;
await middleware.InvokeAsync(workspaceHttp, middlewareUser);
Check(nextCalls == 3 && workspaceHttp.Response.Headers.Location == "/Identity/Account/AccessDenied",
    "Unapproved accounts must not reach protected workspace handlers.");
var failureHttp = new DefaultHttpContext { User = Account(creator.Email) };
failureHttp.Response.Body = new MemoryStream();
middlewareUser.ThrowError = true;
await middleware.InvokeAsync(failureHttp, middlewareUser);
Check(nextCalls == 3 && failureHttp.Response.StatusCode == 503, "Provisioning failure must not allow the request through.");
var logoutHttp = new DefaultHttpContext { RequestServices = provider };
var logout = new BuildWise.Web.Areas.Identity.Pages.Account.LogoutModel(provider.GetRequiredService<CognitoSettings>())
{
    PageContext = new PageContext { HttpContext = logoutHttp }
};
var logoutResult = (RedirectResult)await logout.OnPostAsync();
var logoutLocation = logoutResult.Url;
Check(logoutLocation.StartsWith("https://login.example.test/logout?"), "Logout must use the configured Cognito domain.");
Check(logoutLocation.Contains("logout_uri=" + Uri.EscapeDataString("https://localhost:7213/")), "Logout must return to the configured URL.");
Check(logoutHttp.Response.Headers.SetCookie.ToString().Contains("BuildWise.Auth=;"), "Logout must expire the local authentication cookie.");
Console.WriteLine("Passed: provisioning, duplicate prevention, pending accounts, explicit bootstrap administrator, middleware gating/failure, creator tracking, details and authentication settings.");
Console.WriteLine("Checks used an isolated in-memory database and dummy authentication settings. No real database records or Cognito users were modified.");

sealed class MemoryTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}

sealed class StubCurrentUser : ICurrentUserService
{
    public Guid? Id { get; set; }
    public int Calls { get; private set; }
    public bool ThrowError { get; set; }
    public Task<Guid?> GetUserIdAsync() => Task.FromResult(Id);
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => Task.FromResult(Id);
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => EnsureUserExistsAsync();
    public Task<Guid?> EnsureUserExistsAsync()
    {
        Calls++;
        if (ThrowError) throw new InvalidOperationException("Test database failure");
        return Task.FromResult(Id);
    }
}
