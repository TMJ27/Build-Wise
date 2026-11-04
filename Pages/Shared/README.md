# BuildWise Razor Pages structure

The default ASP.NET Core Razor Pages structure is retained:

```text
Pages/
  Index.cshtml / Index.cshtml.cs
  Error.cshtml / Error.cshtml.cs
  Privacy.cshtml / Privacy.cshtml.cs
  Shared/
  _ViewImports.cshtml
  _ViewStart.cshtml
Areas/
  Configuration/
    Pages/
    ViewModels/
  Customers/
    Pages/
    ViewModels/
  Requests/
    Pages/
    ViewModels/
  Quotations/
    Pages/
    ViewModels/
  Services/
    Pages/
    ViewModels/
ViewModels/
  Dashboard/
  Layout/
Services/
BuildWise.Data/
wwwroot/
```

The dashboard lives at `/`. Module routes include `/Requests`, `/Customers`, `/Quotations`, `/Services`, `/Configuration/Profile/Details` and `/Configuration/Access`. There is no Workspace Area or root redirect to a Workspace route.

Each Area has its own Pages/_ViewImports.cshtml and Pages/_ViewStart.cshtml. All Areas use /Pages/Shared/_Layout.cshtml. Cross-Area navigation specifies asp-area and asp-page; within an Area, relative page names keep the current Area. The shared brand/dashboard link explicitly uses an empty Area. Module view models are stored outside their Pages folders in Areas/<module>/ViewModels. Shared dashboard/layout models remain in the root ViewModels folder.

## Database and current application state

BuildWise.Data.BuildWiseDbContext is registered as a scoped SQL Server context. The development connection string is ConnectionStrings:BuildWise in appsettings.Development.json. Other environments must provide that connection through configuration. Do not inject the scoped context into singleton sample workspaces.

The company profile page uses BuildWiseDbContext directly for the real deployment company. DefaultTerms maps to DefaultQuotationTerms, and the company Guid is preserved between the details and edit pages. The base-currency select reads the Currency table and rejects submitted codes that are not in that register. Its list reloads for invalid submissions. CompanyService is a scoped database reader used by other company-dependent pages.

AccessWorkspace, CatalogueWorkspace, CustomerWorkspace, RequestWorkspace and QuotationWorkspace still keep sample state in memory. Authentication, database persistence for those stores, and external AI/email connections remain separate backend work. AI/email settings do not verify a connection or send mail.

The layout keeps the approved light background, navy sidebar, muted terracotta accents, content-sized buttons and independently scrolling navigation/content panes. No prototype renderer or hash router is loaded.

## Application styling

All application CSS lives in `wwwroot/css/site.css`, loaded once by the shared layout. Pages do not load module stylesheets or define Styles sections. Font Awesome remains a separate vendor dependency.

The root design tokens and shared component rules control typography, buttons, forms, tables, cards, navigation, tabs, alerts and dialogs. Change these shared rules for application-wide adjustments. Page-specific layouts and responsive rules also belong in site.css. Inline chart custom properties supply dynamic data values only.

Previous stylesheets are inactive backups under `design/archived-styles`; edit site.css for current styling.

## Page headings and actions

Every page sets `ViewData["PageTitle"]` and, when needed, `ViewData["PageSubTitle"]`. The shared layout renders the page heading once. Put header buttons, status badges and back links in `@section actionbar`; do not duplicate the heading in the page body. Dynamic heading values are assigned after any local variables they depend on. `Title` remains the browser tab title.

Breadcrumbs follow the CutWise header pattern. Pages supply parent items through `ViewData["Breadcrumbs"]` using `BreadcrumbItem` objects. The shared `_Breadcrumbs` partial adds the Overview link and the current `Title` (or optional `BreadcrumbTitle`). Parent items with local URLs are links; labels such as Configuration remain plain text. On narrow screens the header shows the current breadcrumb. The sidebar reads the current Area and page route to select the active item and open Configuration; pages do not set navigation state in ViewData.

Company profile navigation opens Areas/Configuration/Pages/Profile/Details.cshtml. This read-only page loads SQL Server company data and links to Profile/Edit.cshtml with the saved company Guid. The edit form retains the ID in its bound input, returns to ProfileDetails after saving, and includes Cancel/Back links to the details page. When the company table is empty, the details page offers Add company profile.

Configuration is a native expandable sidebar menu containing Profile, Users, User groups and Currency. It starts open on configuration pages. Users and User groups are read-only SQL-backed registers; account creation and group editing remain separate work. The Currency page adds and edits real currency records using parameterized SQL. Fields are Id (Guid), Name, Code, Symbol, CreatorId (Guid FK to User) and CreationDate. Code and creator are fixed after creation; the app records CreationDate in UTC. The creator selector uses real user records and prefills the authenticated user ID when available. An existing user is required before a currency can be added. database/CreateCurrency.sql creates the table without inserting sample rows.

Configuration modules are grouped under Areas/Configuration/Pages/Profile, Users, UserGroups, Currency and Access. Their view models are grouped in matching folders under Areas/Configuration/ViewModels. Index pages provide module registers; Profile has Details and Edit pages. The root Pages folder and shared layout remain in their standard locations.
