# Configuration modules

```text
Pages/
  Profile/
    Details.cshtml / Details.cshtml.cs
    Edit.cshtml / Edit.cshtml.cs
  Users/
    Index.cshtml / Index.cshtml.cs
  UserGroups/
    Index.cshtml / Index.cshtml.cs
  Currency/
    Index.cshtml / Index.cshtml.cs
  Access/
    Index.cshtml / Index.cshtml.cs
    Member.cshtml / Member.cshtml.cs
    Integration.cshtml / Integration.cshtml.cs
  _ViewImports.cshtml
  _ViewStart.cshtml
ViewModels/
  Profile/
  Users/
  UserGroups/
  Currency/
  Access/
```

Use `asp-area="Configuration"` and a module page path such as `asp-page="/Users/Index"` or `asp-page="/Profile/Details"` for navigation. The root application layout stays in `/Pages/Shared/_Layout.cshtml`.

Page model namespaces follow `BuildWise.Web.Areas.Configuration.Pages.<Module>`. View model namespaces follow `BuildWise.Web.Areas.Configuration.ViewModels.<Module>`.

## Inclusion screens

`Pages/Inclusions` contains Index, Details and Upsert screens with matching view models. Index record links open Details; the Details action bar links to Upsert. All record IDs and edit/detail routes use GUIDs. Navigation uses breadcrumbs without Back or Cancel buttons. Forms and details reuse the shared classes in `wwwroot/css/site.css`, without inline styles. This is UI scaffolding; backend logic remains to be implemented. Request scope screens are in `Areas/Scopes`, outside Configuration.

To connect each module, populate `IndexModel.Records`, load the Details view model and `UpsertModel.Input` on GET for an existing ID, return NotFound for missing records, and implement the POST handler and server-side validation. Enable the disabled submit button in the corresponding Upsert view once saving is implemented. Decide the database relationships and how these reusable entries become quotation snapshots in the backend implementation.
