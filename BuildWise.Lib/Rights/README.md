# Access rights

`AccessRights` defines the permission flags for `UserGroup.RightsId`. It uses `int` to match the current database column. Keep the assigned bit values stable after saving group records.

```csharp
using BuildWise.Lib.Rights;

// Combine the rights selected in the assignment form.
var rights = AccessRights.ViewUsers | AccessRights.CreateUsers;
group.RightsId = (int)rights;

// Read a group's saved rights.
var savedRights = (AccessRights)group.RightsId;
bool canCreateUsers = savedRights.HasFlag(AccessRights.CreateUsers);
```

Each individual flag defines its readable label with `Description` and its module with `Category`. `AccessRightsHelper.GetGroups()` reads those attributes and groups the individual flags for display, excluding zero and composite values such as `All`. Views render these groups without maintaining their own list of rights. Add new flags and their metadata in `AccessRights.cs`; they appear automatically in the assignment form. With no selections, save `None` (zero). `All` combines the currently defined rights.

Defining these flags does not enforce access. Page handlers must check assigned rights when authorization is implemented.
