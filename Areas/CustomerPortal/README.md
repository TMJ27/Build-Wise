# Customer portal

Theme: keep BuildWise's navy/copper palette, DM Sans body text, Manrope headings and shared design tokens. The sibling projects provide layout patterns only; their teal/sage colours and fonts must not replace BuildWise's theme.

The customer landing, registration and login pages reuse the public/account design patterns from the sibling CutWise project, with the public navigation and separate website layout pattern also used by De-Porress. `_CustomerPublicLayout` owns the public document; `_CustomerAuthLayout` shares the account story panel and form card between registration and login. All styles remain scoped in `wwwroot/css/site.css`; `customer-public.js` handles the mobile navigation and illustrative request/quotation tabs. No customer sign-in backend was added.

The public landing page is `/` (`Pages/Welcome`) and allows anonymous access. It links to customer registration/login and staff sign-in. The staff overview is `/Overview`; its Razor page name remains `/Index` so existing page links continue to resolve. The private customer dashboard remains `/CustomerPortal`. Public page styles are in the shared `wwwroot/css/site.css`.

Registration, customer dashboard, request submission/details, approved quotation viewing and logout are implemented. Only credential verification and sign-in in `Pages/Account/Login.cshtml.cs` are intentionally left for the user. Its submit button stays disabled until that handler is implemented. Entry points are `/CustomerPortal/Account/Register`, `/CustomerPortal/Account/Login` and `/CustomerPortal`.

## Database setup

Apply `20261107204457_CustomerPortalAccounts` before running this version, including before staff sign-in: staff authentication now excludes linked customer identities by querying this table. The migration adds CustomerAccount with unique normalized email, ClientId and UserId indexes; no existing records are deleted. It has been generated and checked, not applied to a live database. The SQL artifact is `database/customer-portal/CustomerPortalAccounts.sql` and assumes earlier migrations are already installed. Alternatively use the existing EF migration workflow, such as `Update-Database -Project BuildWise.Data -StartupProject BuildWise.Web` in Visual Studio's Package Manager Console against the intended database.

CustomerAccount stores the salted password hash, normalized email, session security stamp and GUID ClientId. Registration always creates a new Client; it never attaches an existing customer profile by email, because registration does not prove ownership of historical records. It creates an inactive, groupless User audit identity solely because existing Client.CreatorId and ClientRequest.CreatorId require a User foreign key. The audit identity has a random unusable local password and is explicitly excluded from staff authentication, administrator bootstrap and the staff user list/numbering. No customer is granted provider rights. Client profile fields retain existing 50-character limits.

Registration validates the contact fields and a 12–128 character password with confirmation, uses ASP.NET Core PasswordHasher<CustomerAccount> (600,000 iterations), and saves the account/profile/audit identity atomically. Normalized emails are unique and existing staff identities cannot be reused. Anonymous registration is rate-limited to five requests per five minutes per remote IP. Registration redirects to Login without signing in; automatic sign-in is deliberately absent. Existing provider sign-in remains Cognito based.

## Login handoff — implement this part

The customer cookie scheme and identity AuthenticationType must both be `CustomerPortalAuthentication.Scheme` (`CustomerPortal`). The cookie name is `BuildWise.Customer`, separate from the provider cookie. Implement the Login POST using CustomerAccount.NormalizedEmail, the registered IPasswordHasher<CustomerAccount>, active-account/client checks, suitable lockout/rate limits and local-return-URL validation. Password reset and email verification workflows are not included in this change; do not use an unverified email to link existing customer records.

After successful verification, the signed identity needs:

- `ClaimTypes.NameIdentifier`: CustomerAccount.Id (GUID), not ClientId or UserId.
- `CustomerPortalAuthentication.StampClaim` (`buildwise_customer_stamp`): CustomerAccount.SecurityStamp.
- Optional display name/email claims.

Issue the cookie under the CustomerPortal scheme and redirect to `/CustomerPortal`. Cookie validation rechecks account activity, Client activity and SecurityStamp; changing the stamp revokes existing sessions. Enable the disabled button in Login.cshtml when the handler is ready. The login model currently has no POST handler, and no credential verification or cookie issuance code has been supplied.

## Ownership and display

The whole portal area uses the CustomerPortalOnly policy, which authenticates only the customer scheme; Login, Register and POST Logout allow anonymous access. Provider provisioning is skipped for customer-area endpoints. The portal uses its own header/sidebar with the shared layout and site.css. Provider configuration and record-edit links are absent.

CustomerPortalService resolves ownership from the signed account identity and the stored ClientId. Dashboard counts and row queries are filtered by that ID; Request Details and Quotation Details return 404 for foreign GUIDs. New requests take ClientId and audit CreatorId from the authenticated account, never from posted form fields. They appear in the provider Requests and Scope screens.

Only the highest approved revision with an ApprovalDate is shown for each owned quotation. A newer draft remains hidden while the previous approved version stays available. Rows show approved snapshot fields and item amounts. Provider-only scope notes and drafts are not exposed. The dashboard shows the latest ten requests and quotations plus lifetime owned counts. Request/quotation document editing, PDF generation and customer account management are separate features.

Implementation reference: [ASP.NET Core authentication schemes](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0).
