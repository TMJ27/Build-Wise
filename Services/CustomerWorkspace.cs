using BuildWise.Web.ViewModels.Dashboard;

namespace BuildWise.Web.Services;

public sealed class CustomerWorkspace
{
    private readonly object sync = new();
    private readonly Dictionary<int, CustomerEntry> customers = new();
    private int nextId;

    public CustomerWorkspace()
    {
        foreach (var name in DashboardSampleData.Requests.Select(request => request.Customer).Distinct())
        {
            var id = ++nextId;
            var business = name.Contains("Properties") || name.Contains("Offices") || name.Contains("Guesthouse");
            customers[id] = new CustomerEntry(
                id,
                name,
                business ? "Business" : "Individual",
                $"{name.ToLowerInvariant().Replace(' ', '.')}@example.com",
                $"+263 77 123 {4500 + id}",
                "Harare, Zimbabwe",
                string.Empty);
        }
    }

    public IReadOnlyList<CustomerEntry> All
    {
        get
        {
            lock (sync)
            {
                return customers.Values.OrderBy(customer => customer.Name).ToArray();
            }
        }
    }

    public CustomerEntry? Find(int id)
    {
        lock (sync)
        {
            return customers.GetValueOrDefault(id);
        }
    }

    public int? Save(
        int? id,
        string name,
        string type,
        string email,
        string phone,
        string address,
        string notes)
    {
        lock (sync)
        {
            if (id.HasValue && !customers.ContainsKey(id.Value))
            {
                return null;
            }

            if (customers.Values.Any(customer => customer.Id != id &&
                customer.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var customerId = id ?? ++nextId;
            customers[customerId] = new CustomerEntry(
                customerId, name.Trim(), type, email.Trim(), phone.Trim(), address.Trim(), notes.Trim());

            return customerId;
        }
    }

    public CustomerEntry GetOrCreate(string name, string email)
    {
        lock (sync)
        {
            var existing = customers.Values.FirstOrDefault(customer =>
                customer.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                return existing;
            }

            var id = ++nextId;
            var customer = new CustomerEntry(id, name.Trim(), "Individual", email.Trim(), "", "", "");
            customers[id] = customer;
            return customer;
        }
    }
}

public sealed record CustomerEntry(
    int Id,
    string Name,
    string Type,
    string Email,
    string Phone,
    string Address,
    string Notes)
{
    public string CustomerNumber => $"CUS-{Id:D4}";

    public string Initials => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
}
