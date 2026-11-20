using BuildWise.Lib.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Middleware;

public class UserProvisioningMiddleware(RequestDelegate next, ILogger<UserProvisioningMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>()?.AreaName == "CustomerPortal"
            || context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await next(context);
            return;
        }

        Guid? userId;
        try
        {
            userId = await currentUser.EnsureUserExistsAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Local user provisioning failed.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Your account could not be prepared. Please try again later.");
            return;
        }

        if (!userId.HasValue)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/Identity/Account/AccessDenied");
            return;
        }

        await next(context);
    }
}
