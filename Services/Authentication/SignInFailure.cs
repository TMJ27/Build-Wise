using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace BuildWise.Web.Services.Authentication;

public static class SignInFailure
{
    public static string Describe(Exception? failure, string? clientSecret = null)
    {
        // Match known errors, without displaying tokens, provider responses or secrets.
        var messages = string.Empty;
        for (var exception = failure; exception is not null; exception = exception.InnerException)
        {
            messages += " " + exception.Message;
        }

        if (messages.Contains("invalid_client", StringComparison.OrdinalIgnoreCase))
        {
            return "Cognito rejected the app client credentials (invalid_client). Check that the Client ID and Client Secret belong to the same Cognito app client.";
        }

        if (messages.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Cognito rejected the authorization code (invalid_grant). Start a fresh sign-in and check the callback URL.";
        }

        if (messages.Contains("invalid_scope", StringComparison.OrdinalIgnoreCase))
        {
            return "Cognito rejected the requested scopes (invalid_scope). In the app client's login settings, enable the OpenID scopes openid, email and profile, then save the settings.";
        }

        if (messages.Contains("unauthorized_client", StringComparison.OrdinalIgnoreCase))
        {
            return "Cognito does not allow this app client to use the requested flow (unauthorized_client). Enable the authorization code grant in the app client's login settings.";
        }

        if (messages.Contains("Correlation", StringComparison.OrdinalIgnoreCase))
        {
            return "The sign-in correlation cookie was missing or invalid. Start again from https://localhost:7213 and keep the same browser and host throughout sign-in.";
        }

        if (messages.Contains("nonce", StringComparison.OrdinalIgnoreCase))
        {
            return "The sign-in nonce could not be validated. Close old sign-in tabs and start a fresh sign-in.";
        }

        if (messages.Contains("redirect_mismatch", StringComparison.OrdinalIgnoreCase)
            || messages.Contains("redirect_uri", StringComparison.OrdinalIgnoreCase))
        {
            return "Check the Cognito allowed callback URL. For the HTTPS launch profile it must be https://localhost:7213/signin-oidc.";
        }

        if (messages.Contains("IDX20803", StringComparison.OrdinalIgnoreCase)
            || messages.Contains("IDX20804", StringComparison.OrdinalIgnoreCase))
        {
            return "BuildWise could not load Cognito's OpenID configuration. Check the Authority URL and network connection.";
        }

        if (messages.Contains("access_denied", StringComparison.OrdinalIgnoreCase))
        {
            return "The identity provider denied or cancelled sign-in (access_denied).";
        }

        // IdentityModel formats protocol errors as error: '...', error_description: '...'.
        // Extract these fields rather than exposing the full response or token data.
        var code = Regex.Match(messages, @"\berror:\s*'([^']{1,100})'", RegexOptions.IgnoreCase);
        var description = Regex.Match(messages, @"\berror_description:\s*'([^']{1,400})'", RegexOptions.IgnoreCase);
        if (code.Success)
        {
            return description.Success
                ? $"Cognito error: {code.Groups[1].Value}. Description: {description.Groups[1].Value}."
                : $"Cognito error: {code.Groups[1].Value}.";
        }

        if (failure?.GetBaseException() is OpenIdConnectProtocolException protocolError)
        {
            var detail = protocolError.Message;
            if (!string.IsNullOrEmpty(clientSecret))
            {
                detail = detail.Replace(clientSecret, "[redacted]", StringComparison.Ordinal);
            }

            detail = Regex.Replace(detail, @"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", "[redacted token]");
            detail = Regex.Replace(detail,
                @"(client_secret|access_token|refresh_token|id_token|code)\s*[=:]\s*[^\s,&]+",
                "$1=[redacted]", RegexOptions.IgnoreCase);
            return "OpenID Connect protocol error: " + detail[..Math.Min(detail.Length, 800)];
        }

        return $"Sign-in failed with {failure?.GetBaseException().GetType().Name ?? "an unknown error"}. The full exception is logged under BuildWise.Authentication in the server output.";
    }
}
