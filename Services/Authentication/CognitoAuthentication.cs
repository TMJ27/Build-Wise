using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Rights;
using BuildWise.Lib.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.Authentication;

public static class CognitoAuthentication
{
    public static void AddCognitoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("Cognito").Get<CognitoSettings>() ?? new();
        services.AddSingleton(settings);

        var authentication = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.Cookie.Name = "BuildWise.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = async context =>
                {
                    // Signing out must remain available when the local account or database is unavailable.
                    if (HttpMethods.IsPost(context.Request.Method)
                        && context.Request.Path.Equals("/Identity/Account/Logout", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
                    if (context.Principal is null || !(await currentUser.GetUserIdAsync(context.Principal)).HasValue)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

        if (settings.IsConfigured)
        {
            authentication.AddOpenIdConnect(options =>
            {
                options.Authority = settings.Authority.TrimEnd('/');
                options.ClientId = settings.ClientId;
                options.ClientSecret = settings.ClientSecret;
                options.CallbackPath = "/signin-oidc";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.UsePkce = true;
                options.MapInboundClaims = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.TokenValidationParameters.NameClaimType = "email";
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");

                options.Events.OnTicketReceived = async context =>
                {
                    var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
                    var userId = context.Principal is null
                        ? null
                        : await currentUser.EnsureUserExistsAsync(context.Principal);

                    if (!userId.HasValue || context.Principal?.Identity is not ClaimsIdentity identity)
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/Identity/Account/AccessDenied");
                        return;
                    }

                    identity.AddClaim(new Claim("buildwise_user_id", userId.Value.ToString()));

                    var db = context.HttpContext.RequestServices.GetRequiredService<BuildWiseDbContext>();
                    var isAdministrator = await db.Users.AnyAsync(user => user.Id == userId.Value
                        && user.Group != null && user.Group.RightsId == (int)AccessRights.All);

                    if (isAdministrator && !await db.Companies.AnyAsync())
                    {
                        context.ReturnUri = "/Configuration/Profile/Edit";
                    }
                };

                options.Events.OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("BuildWise.Authentication");

                    logger.LogError(context.Failure,
                        "Cognito sign-in failed. Trace identifier: {TraceIdentifier}",
                        context.HttpContext.TraceIdentifier);

                    var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
                    if (environment.IsDevelopment())
                    {
                        var tempData = context.HttpContext.RequestServices
                            .GetRequiredService<ITempDataDictionaryFactory>()
                            .GetTempData(context.HttpContext);

                        tempData["SignInDiagnostic"] = SignInFailure.Describe(context.Failure, settings.ClientSecret);
                        tempData.Save();
                    }

                    context.HandleResponse();
                    context.Response.Redirect("/Identity/Account/Login?error=signin");
                    return Task.CompletedTask;
                };

            });
        }

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}
