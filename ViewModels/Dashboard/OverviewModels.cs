using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Services.Requests;

namespace BuildWise.Web.ViewModels.Dashboard;

public sealed class OverviewRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public int StatusId { get; set; }
    public DateTime CreationDate { get; set; }
    public int ScopeItemCount { get; set; }
    public int PendingScopeCount { get; set; }
    public string RequestNumber => ReferenceNumber ?? Id.ToString();
    public string Status => RequestDisplay.StatusName(StatusId);
    public string BadgeClass => StatusId == (int)ClientRequestStatus.Pending_Verification ? "amber" : string.Empty;
    public DateOnly CreatedOn => DateOnly.FromDateTime(CreationDate.AddHours(2));
}

public sealed class OverviewQuotation
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public QuotationStatus? Status { get; set; }
    public bool IncludesPrices { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Total { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateOnly? PreparedOn => CreationDate.HasValue ? DateOnly.FromDateTime(CreationDate.Value.AddHours(2)) : null;
}

public sealed record OverviewCategory(string Name, int Count, int ColourIndex)
{
    private static readonly string[] Colours = ["#637d9d", "#aa6248", "#839a8e", "#a394b6", "#a5b6c7", "#cbd5e1"];
    public string Colour => Colours[ColourIndex];
    public string ColourClass => $"colour-{ColourIndex}";
}

public sealed record OverviewAttention(string Title, string Tone, string Route, OverviewRequest Request);

public sealed record OverviewActivity(string Title, string Detail, string Route, DateTime Timestamp)
{
    public DateOnly Date => DateOnly.FromDateTime(Timestamp.AddHours(2));
}
