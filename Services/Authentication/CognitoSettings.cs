namespace BuildWise.Web.Services.Authentication;

public sealed class CognitoSettings
{
    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public string Domain { get; set; } = string.Empty;

    public string LogoutUri { get; set; } = string.Empty;

    public string BootstrapAdminEmail { get; set; } = string.Empty;

    public bool IsConfigured => Uri.TryCreate(Authority, UriKind.Absolute, out var authority)
        && authority.Scheme == Uri.UriSchemeHttps
        && !string.IsNullOrWhiteSpace(ClientId)
        && Uri.TryCreate(Domain, UriKind.Absolute, out var domain)
        && domain.Scheme == Uri.UriSchemeHttps
        && Uri.TryCreate(LogoutUri, UriKind.Absolute, out var signedOutUri)
        && signedOutUri.Scheme == Uri.UriSchemeHttps;
}
