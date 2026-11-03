namespace BuildWise.Web.ViewModels.Dashboard;

public sealed record DashboardRequest(int Id, string Title, string Customer, string Category, string Status, DateOnly CreatedOn)
{
    public string BadgeClass => Status switch
    {
        "Needs clarification" => "amber", "Scope review" => "purple", "Ready to quote" => "green",
        "Quotation approved" => "green", _ => string.Empty
    };
}

public sealed record DashboardQuotation(int Id, int RequestId, string Status, decimal Amount, int Revision, DateOnly PreparedOn);
public sealed record DashboardActivity(string Title, string Detail, string Icon, string Route, DateOnly Date);
public sealed record DashboardCategory(string Name, int Count, int Percentage, string Colour);
public sealed record DashboardDay(DateOnly Date, int Prepared, int Approved);
public sealed record DashboardStage(string Name, string Status, string Route, int Count, int? FirstRequestId);
public sealed record DashboardAttention(string Title, string Icon, string Tone, string Route, DashboardRequest Request);

// Explicit sample records for the server-rendered dashboard; replace this source
// with a provider-scoped query service when database integration is implemented.
public static class DashboardSampleData
{
    public static readonly DateOnly Today = new(2026, 11, 2);

    public static IReadOnlyList<DashboardRequest> Requests { get; } = Array.AsReadOnly(new[]
    {
        new DashboardRequest(1042, "Electrical installation", "Tendai Moyo", "Electrical", "Needs clarification", new(2026,11,1)),
        new DashboardRequest(1041, "Interior painting", "Sarah Chikore", "Painting", "Scope review", new(2026,10,31)),
        new DashboardRequest(1040, "Office plumbing repairs", "Westgate Properties", "Plumbing", "Ready to quote", new(2026,10,30)),
        new DashboardRequest(1039, "Kitchen renovation", "David Nyathi", "Renovations", "Draft quotation", new(2026,10,29)),
        new DashboardRequest(1038, "Bathroom plumbing upgrade", "Rudo Mutasa", "Plumbing", "Needs clarification", new(2026,11,2)),
        new DashboardRequest(1037, "Office electrical assessment", "Oakfield Offices", "Electrical", "Scope review", new(2026,11,2)),
        new DashboardRequest(1036, "Exterior wall painting", "Simba Dube", "Painting", "Ready to quote", new(2026,11,1)),
        new DashboardRequest(1035, "Kitchen cabinet refurbishment", "Chiedza Ncube", "Renovations", "Draft quotation", new(2026,11,1)),
        new DashboardRequest(1034, "Guesthouse maintenance", "Pinewood Guesthouse", "Maintenance", "Quotation approved", new(2026,10,31)),
        new DashboardRequest(1033, "Kitchen plumbing repairs", "Farai Chuma", "Plumbing", "Quotation approved", new(2026,10,30)),
        new DashboardRequest(1032, "Guest room painting", "Pinewood Guesthouse", "Painting", "Quotation approved", new(2026,10,29)),
        new DashboardRequest(1031, "Office lighting upgrade", "Oakfield Offices", "Electrical", "Quotation approved", new(2026,10,28))
    });

    public static IReadOnlyList<DashboardQuotation> Quotations { get; } = Array.AsReadOnly(new[]
    {
        new DashboardQuotation(128,1040,"Approved",480m,1,new(2026,10,31)),
        new DashboardQuotation(127,1039,"Draft",2450m,2,new(2026,10,29)),
        new DashboardQuotation(126,1035,"Draft",1850m,1,new(2026,11,2)),
        new DashboardQuotation(125,1034,"Approved",1260m,2,new(2026,11,1)),
        new DashboardQuotation(124,1033,"Approved",375m,1,new(2026,10,31)),
        new DashboardQuotation(123,1032,"Approved",980m,1,new(2026,10,30)),
        new DashboardQuotation(122,1031,"Approved",1420m,1,new(2026,10,28))
    });
}
