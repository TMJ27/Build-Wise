using System.Security.Claims;
using BuildWise.Lib.Users;
using BuildWise.Web.Areas.Identity.Pages.Account;
using BuildWise.Web.Middleware;
using BuildWise.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var services = new ServiceCollection();
services.AddLogging();
services.AddDataProtection().UseEphemeralDataProtectionProvider();
services.AddSingleton<ICurrentUserService, UnavailableUserService>();
services.AddCognitoAuthentication(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Cognito:Authority"] = "https://issuer.example.test/pool",
    ["Cognito:Domain"] = "https://login.example.test/",
    ["Cognito:ClientId"] = "test-client",
    ["Cognito:LogoutUri"] = "https://localhost:7213/"
}).Build());
using var provider = services.BuildServiceProvider();

// Real cookie handler must delete the local cookie even if no active session remains.
foreach (var authenticated in new[] { true, false })
{
    using var scope = provider.CreateScope();
    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    http.Request.Scheme = "https";
    http.Request.Method = "POST";
    http.Request.Path = "/Identity/Account/Logout";
    if (authenticated) http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "user@example.test")], "Cookies"));
    var options = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
    var ticket = new AuthenticationTicket(http.User, new AuthenticationProperties(), "Cookies");
    await options.Events.ValidatePrincipal(new CookieValidatePrincipalContext(http, new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)), options, ticket));

    var page = new LogoutModel(provider.GetRequiredService<CognitoSettings>())
    {
        PageContext = new PageContext { HttpContext = http }
    };
    var result = (RedirectResult)await page.OnPostAsync();
    Check(http.Response.Headers.SetCookie.ToString().Contains("BuildWise.Auth=;"), "Local cookie was not expired.");
    var target = new Uri(result.Url);
    var query = QueryHelpers.ParseQuery(target.Query);
    Check(target.Host == "login.example.test" && target.AbsolutePath == "/logout", "Wrong Cognito logout endpoint.");
    Check(query["client_id"] == "test-client" && query["logout_uri"] == "https://localhost:7213/", "Cognito parameters were not preserved.");
}

var anonymous = Attribute.IsDefined(typeof(LogoutModel), typeof(AllowAnonymousAttribute));
Check(anonymous, "Expired or disabled accounts must still be able to sign out.");
var middlewareHttp = new DefaultHttpContext
{
    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "user@example.test")], "Cookies"))
};
middlewareHttp.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "Logout"));
var continued = false;
await new UserProvisioningMiddleware(_ => { continued = true; return Task.CompletedTask; }, NullLogger<UserProvisioningMiddleware>.Instance)
    .InvokeAsync(middlewareHttp, new UnavailableUserService());
Check(continued, "Provisioning blocked logout when the database was unavailable.");

var localHttp = new DefaultHttpContext { RequestServices = provider };
var localPage = new LogoutModel(new CognitoSettings()) { PageContext = new PageContext { HttpContext = localHttp } };
Check(await localPage.OnPostAsync() is RedirectToPageResult { PageName: "/Account/Login" }, "Unconfigured Cognito must still allow local logout.");
Console.WriteLine("PASS: cookie deletion, Cognito redirect, expired sessions, unavailable database and local-only logout.");

sealed class UnavailableUserService : ICurrentUserService
{
    public Task<Guid?> GetUserIdAsync() => throw new Exception("Database is unavailable.");
    public Task<Guid?> GetUserIdAsync(ClaimsPrincipal principal) => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync() => GetUserIdAsync();
    public Task<Guid?> EnsureUserExistsAsync(ClaimsPrincipal principal) => GetUserIdAsync();
}
