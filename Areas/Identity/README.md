# Cognito sign-in setup

The Web project uses ASP.NET Core cookies and OpenID Connect authorization code flow with PKCE. Configuration is read from the `Cognito` section. Public settings may be saved in appsettings.Development.json:

```json
{
  "Cognito": {
    "Authority": "https://cognito-idp.REGION.amazonaws.com/USER_POOL_ID",
    "ClientId": "YOUR_APP_CLIENT_ID",
    "Domain": "https://YOUR_LOGIN_DOMAIN",
    "LogoutUri": "https://localhost:7213/"
  }
}
```

For an app client with a secret, right-click **BuildWise.Web** in Visual Studio and choose **Manage User Secrets**. Save the secret in that project's store:

```json
{
  "Cognito:ClientSecret": "YOUR_APP_CLIENT_SECRET"
}
```

Secrets saved for CutWise, Deporess, or a different project are not loaded by BuildWise.Web. Restart the app after configuration changes. Use the HTTPS launch profile, whose port is 7213. Development User Secrets are not loaded in production; configure Cognito__ClientSecret through the deployment's secret configuration there.

In Cognito, allow `https://localhost:7213/signin-oidc` as the callback/default redirect, and `https://localhost:7213/` as the sign-out URL. The sign-out URL must exactly match `Cognito:LogoutUri`, including its path and trailing slash. Enable authorization code grant and the openid, email and profile scopes. Use the actual production URLs when deploying.

The managed-login domain is the Cognito hosted/custom login domain; the Authority is the user pool issuer URL. The app uses /signin-oidc as the middleware callback, not a Razor Page. Logout starts with an antiforgery-protected POST, deletes the local authentication cookie and redirects the browser directly to Cognito's `/logout` endpoint. It remains available for expired or disabled accounts and does not depend on database access or OpenID discovery. Cognito then returns to the configured LogoutUri. Protected pages redirect to the local Login page rather than automatically starting another Cognito session after sign-out.

Verified Cognito sign-ins automatically create a local User row if the email is new. After the initial administrator is established, new accounts are inactive and have no group until approved; inactive accounts are not automatically reactivated. The sign-in event uses the shared provisioning service before issuing a cookie. UserProvisioningMiddleware runs after authentication and before authorization and uses the same service for authenticated workspace requests. Existing active accounts are reused; provisioning does not duplicate rows or reset group assignments. LastLoginDate updates on successful sign-in, not on every page request. Unverified emails never provision an account. The current database limits email to 50 characters, so longer emails cannot be provisioned without a schema change.

On a fresh company deployment, the first verified sign-in becomes the active administrator and creates the Administrators group with all defined rights and a real CreatorId. No predefined email is required. If that account already exists as pending, setup reuses and activates it. Setup is unavailable once any user is active, has an activation/login date, or an all-rights group exists; deactivating the administrator does not reopen setup. SQL Server provisioning uses a serializable transaction. An optional `Cognito:BootstrapAdminEmail` can restrict the initial setup to a specified email if required for deployment. Until a company profile exists, administrator sign-ins open `/Configuration/Profile/Edit`. Arrange the first sign-in with the company owner before opening a fresh deployment to general use.

The current-user service resolves the local Guid and rechecks active status when validating the authentication cookie. User group creation saves this Guid as CreatorId and records CreationDate in UTC; edits preserve both fields. Provisioned accounts receive a random, unrecoverable local password hash to satisfy the current non-null column; passwords remain managed by Cognito.

Authentication protects workspace pages. The rights enum and group assignment form define permissions; per-action rights authorization remains separate work.
