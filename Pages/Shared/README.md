# BuildWise native Razor workspace

## Database access

The scaffolded BuildWise.Data.BuildWiseDbContext is registered in Program.cs with AddDbContext and the SQL Server provider. It is scoped to each web request; do not store it in a static field or inject it into the singleton sample workspaces. Inject BuildWiseDbContext into page models or scoped services when replacing the sample stores with database queries.

The development connection string is ConnectionStrings:BuildWise in appsettings.Development.json and targets .\SQLEXPRESS / Build-Wise with Windows authentication. Other environments must provide ConnectionStrings__BuildWise through environment configuration or an equivalent configuration source. Registration does not create, migrate or change the database. The existing pages still use sample stores until individually connected.

Example constructor injection and query:

```csharp
private readonly BuildWiseDbContext _database;

public IndexModel(BuildWiseDbContext database)
{
    _database = database;
}

// Inside an asynchronous page handler (requires Microsoft.EntityFrameworkCore):
var clients = await _database.Clients.AsNoTracking().ToListAsync();
```

The dashboard is rendered by Pages/Index.cshtml and its page model. Requests are rendered by Pages/Requests/Index.cshtml, with native Details and Create pages in that folder. The shared layout loads blueprint.css for the approved colour scheme and workspace.css for the sidebar, header, buttons and independent scrolling.

Navigation uses Razor page URLs. No prototype renderer or hash router is loaded. Navigation items whose native pages have not been implemented remain visible as disabled labels until those pages are added.

CompanyWorkspace, AccessWorkspace, CustomerWorkspace, RequestWorkspace, QuotationWorkspace and CatalogueWorkspace are shared, in-memory sample sources registered as singletons. Records are validated server-side and appear in their registers and the dashboard during the current application run; changes reset when the application restarts. Replace these stores with database persistence when backend integration is implemented.

Access and integrations uses Pages/Configuration/Access.cshtml, Member.cshtml and Integration.cshtml. Team member settings use Guid IDs, duplicate email validation, owner-edit protection and concurrent-save checks. The AI and email pages save non-secret preferences and continue to show Not connected; saving does not verify a connection or send an email. Authentication, actual role enforcement and external AI/email connections are not implemented by these design pages.

Business profile configuration uses Pages/Configuration/Profile.cshtml. It supports company contact/registration details, PNG/JPEG logo uploads up to 2 MB, logo removal, validity defaults, terms and exclusions. The page includes a live document identity preview and protects against stale concurrent saves. The quotation currency remains USD, matching the existing pricing screens. New quotations copy the company configuration and logo; existing quotations retain their company snapshot. New quotation editors prefill the saved terms/exclusions, and previews show the stored company identity and validity. The profile and uploaded logo are held in memory until SQL Server/storage integration is implemented.

The service catalogue uses Pages/Services/Index.cshtml, Details.cshtml and Edit.cshtml. It supports active/all/archived views, search and category filters, service definitions, inclusions/exclusions, clarification prompts, work stages, dependencies and availability changes. New catalogue records use Guid IDs. Duplicate service codes and circular dependencies are rejected. A service's Start request link prefills its name and category on the native request form. The scaffolded BuildWise.Data entities are not altered by this design implementation, and these screens do not yet write to SQL Server.

Customers use Pages/Customers/Index.cshtml, Details.cshtml and Edit.cshtml. The directory supports search, customer type filters, sorting and pagination. Profiles include contact details, notes, requests and quotations. Request records store a stable CustomerId, so renaming a customer preserves their work history and updates customer names on existing request and quotation views. New request forms can prefill contact details using the customerId query parameter.

Quotations use Pages/Quotations/Index.cshtml, Details.cshtml, Edit.cshtml and Preview.cshtml. The editor supports draft creation and edits, item addition/removal, terms and exclusions. Only requests ready to quote and without an existing quotation can start a new quotation. Draft approval requires an explicit provider confirmation and updates the linked request status. Approved quotations cannot be edited. The preview supports the browser's Print / Save PDF action through dedicated print styles.
