using System.Security.Claims;
using System.Threading.RateLimiting;
using BuildWise.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace BuildWise.Web.Services.CustomerPortal;

public static class CustomerPortalAuthentication
{
    public const string Scheme = "CustomerPortal";
    public const string Policy = "CustomerPortalOnly";
    public const string StampClaim = "buildwise_customer_stamp";

    public static void AddCustomerPortal(this IServiceCollection services)
    {
        services.AddScoped<CustomerPortalService>();
        services.AddScoped<CustomerRegistrationService>();
        services.AddScoped<IPasswordHasher<CustomerAccount>, PasswordHasher<CustomerAccount>>();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 600_000);
        // Cookie infrastructure only: the login handler is intentionally left for the user.
        services.AddAuthentication().AddCookie(Scheme, options =>
        {
            options.LoginPath = "/CustomerPortal/Account/Login";
            options.AccessDeniedPath = "/CustomerPortal/Account/Login";
            options.Cookie.Name = "BuildWise.Customer";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Events.OnValidatePrincipal = async context =>
            {
                if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals("/CustomerPortal/Account/Logout")) return;
                var portal = context.HttpContext.RequestServices.GetRequiredService<CustomerPortalService>();
                if (context.Principal is null || await portal.GetAccountAsync(context.Principal) is null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(Scheme);
                }
            };
        });
        services.AddAuthorization(options => options.AddPolicy(Policy, policy =>
        {
            policy.AddAuthenticationSchemes(Scheme);
            policy.RequireAuthenticatedUser();
            policy.RequireClaim(ClaimTypes.NameIdentifier);
            policy.RequireClaim(StampClaim);
        }));
        services.AddRazorPages(options => options.Conventions.AuthorizeAreaFolder("CustomerPortal", "/", Policy));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("CustomerRegistration", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0
                }));
        });
    }
}
