using System.Collections.Concurrent;
using BuildWise.Web.ViewModels.Dashboard;

namespace BuildWise.Web.Services;

// Shared sample workspace for the UI; replace with provider-scoped persistence later.
public sealed class RequestWorkspace
{
    private readonly CustomerWorkspace customers;
    private readonly ConcurrentDictionary<int, RequestEntry> entries = new();
    private int nextId = 1042;

    public RequestWorkspace(CustomerWorkspace customers)
    {
        this.customers = customers;

        foreach (var request in DashboardSampleData.Requests)
        {
            var customer = customers.GetOrCreate(request.Customer, string.Empty);
            entries[request.Id] = new RequestEntry(
                request,
                "Harare",
                customer.Email,
                $"{request.Title} requested by {request.Customer}. Confirm site conditions, measurements and materials before preparing the scope.",
                customer.Id);
        }
    }

    public int CustomerCount => customers.All.Count;

    public IReadOnlyList<DashboardRequest> Requests => entries.Values
        .Select(entry => Current(entry).Request)
        .OrderByDescending(request => request.CreatedOn)
        .ThenByDescending(request => request.Id)
        .ToArray();

    public IReadOnlyList<DashboardRequest> ForCustomer(int customerId) => entries.Values
        .Where(entry => entry.CustomerId == customerId)
        .Select(entry => Current(entry).Request)
        .OrderByDescending(request => request.CreatedOn)
        .ThenByDescending(request => request.Id)
        .ToArray();

    public RequestEntry? Find(int id)
    {
        return entries.TryGetValue(id, out var entry) ? Current(entry) : null;
    }

    public void SetStatus(int id, string status)
    {
        if (entries.TryGetValue(id, out var entry))
        {
            entries.TryUpdate(
                id,
                entry with { Request = entry.Request with { Status = status } },
                entry);
        }
    }

    public int Create(string title, string customerName, string category, string location, string email, string description)
    {
        var customer = customers.GetOrCreate(customerName, email);
        var id = Interlocked.Increment(ref nextId);
        var request = new DashboardRequest(
            id,
            title.Trim(),
            customer.Name,
            category,
            "Needs clarification",
            DateOnly.FromDateTime(DateTime.Today));

        entries[id] = new RequestEntry(request, location.Trim(), email.Trim(), description.Trim(), customer.Id);
        return id;
    }

    private RequestEntry Current(RequestEntry entry)
    {
        var customer = customers.Find(entry.CustomerId);

        return customer is null
            ? entry
            : entry with
            {
                Request = entry.Request with { Customer = customer.Name },
                Email = customer.Email
            };
    }
}

public sealed record RequestEntry(
    DashboardRequest Request,
    string Location,
    string Email,
    string Description,
    int CustomerId);
