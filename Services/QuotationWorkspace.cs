using BuildWise.Web.ViewModels.Dashboard;
using BuildWise.Web.Areas.Quotations.ViewModels;

namespace BuildWise.Web.Services;

// Sample workspace state is shared by the native pages for the current app run.
public sealed class QuotationWorkspace
{
    private readonly object sync = new();
    private readonly Dictionary<int, QuotationEntry> quotations;
    private int nextId = 128;

    public QuotationWorkspace()
    {
        quotations = DashboardSampleData.Quotations.ToDictionary(
            quotation => quotation.Id,
            quotation => new QuotationEntry(
                quotation,
                [new QuotationLine("Labour and materials as specified in the agreed scope", 1, quotation.Amount)],
                "Valid for 14 days. Work starts after the scope, access and payment arrangements are confirmed.",
                "Additional work and changes to the agreed scope will be quoted separately.",
                null));
    }

    public IReadOnlyList<QuotationEntry> All
    {
        get
        {
            lock (sync)
            {
                return quotations.Values
                    .OrderByDescending(entry => entry.Quotation.PreparedOn)
                    .ThenByDescending(entry => entry.Quotation.Id)
                    .ToArray();
            }
        }
    }

    public QuotationEntry? Find(int id)
    {
        lock (sync)
        {
            return quotations.GetValueOrDefault(id);
        }
    }

    public int? Save(
        int? id,
        int requestId,
        IReadOnlyList<QuotationLine> lines,
        string terms,
        string exclusions,
        CompanySnapshotViewModel companySnapshot)
    {
        lock (sync)
        {
            var existing = id.HasValue ? quotations.GetValueOrDefault(id.Value) : null;

            if (id.HasValue && (existing is null || existing.Quotation.Status != "Draft"))
            {
                return null;
            }

            if (!id.HasValue && quotations.Values.Any(entry => entry.Quotation.RequestId == requestId))
            {
                return null;
            }

            var quotationId = id ?? ++nextId;
            var quotation = new DashboardQuotation(
                quotationId,
                existing?.Quotation.RequestId ?? requestId,
                "Draft",
                lines.Sum(line => line.Total),
                existing is null ? 1 : existing.Quotation.Revision + 1,
                DateOnly.FromDateTime(DateTime.Today));

            quotations[quotationId] = new QuotationEntry(
                quotation,
                Array.AsReadOnly(lines.ToArray()),
                terms.Trim(),
                exclusions.Trim(),
                existing?.CompanySnapshot ?? companySnapshot);

            return quotationId;
        }
    }

    public bool Approve(int id)
    {
        lock (sync)
        {
            if (!quotations.TryGetValue(id, out var entry) || entry.Quotation.Status != "Draft")
            {
                return false;
            }

            quotations[id] = entry with
            {
                Quotation = entry.Quotation with { Status = "Approved" }
            };

            return true;
        }
    }
}

public sealed record QuotationLine(string Description, decimal Quantity, decimal Rate)
{
    public decimal Total => decimal.Round(Quantity * Rate, 2, MidpointRounding.AwayFromZero);
}

public sealed record QuotationEntry(
    DashboardQuotation Quotation,
    IReadOnlyList<QuotationLine> Lines,
    string Terms,
    string Exclusions,
    CompanySnapshotViewModel? CompanySnapshot)
{
    public string Number => $"BW-2026-{Quotation.Id:D4}";
}
