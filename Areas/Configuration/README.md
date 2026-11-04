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
