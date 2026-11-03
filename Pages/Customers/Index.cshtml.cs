using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Customers;

public class IndexModel(CustomerWorkspace customers, RequestWorkspace requests) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public IReadOnlyList<CustomerRow> All { get; private set; } = [];
    public IReadOnlyList<CustomerRow> Records { get; private set; } = [];
    public int Total { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / 8d));
    public int BusinessCount => All.Count(row => row.Customer.Type == "Business");
    public int IndividualCount => All.Count(row => row.Customer.Type == "Individual");
    public int ActiveCount => All.Count(row => row.OpenRequests > 0);

    public void OnGet()
    {
        Search = (Search ?? string.Empty).Trim();

        if (Search.Length > 120)
        {
            Search = Search[..120];
        }

        Type = Type is "Individual" or "Business" ? Type : "all";
        Sort = Sort is "requests" or "recent" ? Sort : "name";

        All = customers.All.Select(customer =>
        {
            var related = requests.ForCustomer(customer.Id);
            return new CustomerRow(
                customer,
                related.Count,
                related.Count(request => request.Status != "Quotation approved"),
                related.Select(request => (DateOnly?)request.CreatedOn).Max());
        }).ToArray();

        var filtered = All.Where(row =>
            (Type == "all" || row.Customer.Type == Type) &&
            $"{row.Customer.Name} {row.Customer.Email} {row.Customer.Phone} {row.Customer.Address}"
                .Contains(Search, StringComparison.OrdinalIgnoreCase));

        Total = filtered.Count();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);

        var ordered = Sort switch
        {
            "requests" => filtered.OrderByDescending(row => row.RequestCount).ThenBy(row => row.Customer.Name),
            "recent" => filtered.OrderByDescending(row => row.LastRequest).ThenBy(row => row.Customer.Name),
            _ => filtered.OrderBy(row => row.Customer.Name)
        };

        Records = ordered.Skip((PageNumber - 1) * 8).Take(8).ToArray();
        ModelState.Clear();
    }
}

public sealed record CustomerRow(CustomerEntry Customer, int RequestCount, int OpenRequests, DateOnly? LastRequest);
