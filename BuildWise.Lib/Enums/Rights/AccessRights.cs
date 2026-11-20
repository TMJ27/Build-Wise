using System;
using System.ComponentModel;

namespace BuildWise.Lib.Enums.Rights
{
    [Flags]
    public enum AccessRights : int
    {
        [Description("No access")]
        None = 0,

        // Values are stored in UserGroup.RightsId. Never change or reuse assigned bits.

        // Dashboard
        [Category("Dashboard")]
        [Description("View dashboard")]
        ViewDashboard = 1 << 0,

        // Requests
        [Category("Requests")]
        [Description("View requests")]
        ViewRequests = 1 << 1,

        [Category("Requests")]
        [Description("Create requests")]
        CreateRequests = 1 << 2,

        [Category("Requests")]
        [Description("Edit requests")]
        EditRequests = 1 << 3,

        // Quotations
        [Category("Quotations")]
        [Description("View quotations")]
        ViewQuotations = 1 << 4,

        [Category("Quotations")]
        [Description("Create quotations")]
        CreateQuotations = 1 << 5,

        [Category("Quotations")]
        [Description("Edit quotations")]
        EditQuotations = 1 << 6,

        [Category("Quotations")]
        [Description("Approve quotations")]
        ApproveQuotations = 1 << 7,

        [Category("Quotations")]
        [Description("Share quotations")]
        ShareQuotations = 1 << 8,

        // Customers
        [Category("Customers")]
        [Description("View customers")]
        ViewCustomers = 1 << 9,

        [Category("Customers")]
        [Description("Create customers")]
        CreateCustomers = 1 << 10,

        [Category("Customers")]
        [Description("Edit customers")]
        EditCustomers = 1 << 11,

        // Services
        [Category("Services")]
        [Description("View services")]
        ViewServices = 1 << 12,

        [Category("Services")]
        [Description("Create services")]
        CreateServices = 1 << 13,

        [Category("Services")]
        [Description("Edit services")]
        EditServices = 1 << 14,

        // Company profile
        [Category("Company profile")]
        [Description("View company profile")]
        ViewCompanyProfile = 1 << 15,

        [Category("Company profile")]
        [Description("Edit company profile")]
        EditCompanyProfile = 1 << 16,

        // Users
        [Category("Users")]
        [Description("View users")]
        ViewUsers = 1 << 17,

        [Category("Users")]
        [Description("Create users")]
        CreateUsers = 1 << 18,

        [Category("Users")]
        [Description("Edit users")]
        EditUsers = 1 << 19,

        // User groups
        [Category("User groups")]
        [Description("View user groups")]
        ViewUserGroups = 1 << 20,

        [Category("User groups")]
        [Description("Create user groups")]
        CreateUserGroups = 1 << 21,

        [Category("User groups")]
        [Description("Edit user groups")]
        EditUserGroups = 1 << 22,

        // Currencies
        [Category("Currencies")]
        [Description("View currencies")]
        ViewCurrencies = 1 << 23,

        [Category("Currencies")]
        [Description("Create currencies")]
        CreateCurrencies = 1 << 24,

        [Category("Currencies")]
        [Description("Edit currencies")]
        EditCurrencies = 1 << 25,

        // Integrations
        [Category("Integrations")]
        [Description("View integrations")]
        ViewIntegrations = 1 << 26,

        [Category("Integrations")]
        [Description("Edit integrations")]
        EditIntegrations = 1 << 27,

        [Description("All access")]
        All = ViewDashboard
            | ViewRequests | CreateRequests | EditRequests
            | ViewQuotations | CreateQuotations | EditQuotations | ApproveQuotations | ShareQuotations
            | ViewCustomers | CreateCustomers | EditCustomers
            | ViewServices | CreateServices | EditServices
            | ViewCompanyProfile | EditCompanyProfile
            | ViewUsers | CreateUsers | EditUsers
            | ViewUserGroups | CreateUserGroups | EditUserGroups
            | ViewCurrencies | CreateCurrencies | EditCurrencies
            | ViewIntegrations | EditIntegrations
    }
}
