using BuildWise.Web.Services;
using BuildWise.Web.ViewModels.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Customers;

public class DetailsModel(
    CustomerWorkspace customers,
    RequestWorkspace requests,
    QuotationWorkspace quotations) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "requests";

    public CustomerEntry Customer { get; private set; } = null!;
    public IReadOnlyList<DashboardRequest> Requests { get; private set; } = [];
    public IReadOnlyList<QuotationEntry> Quotations { get; private set; } = [];
    public int OpenCount => Requests.Count(request => request.Status != "Quotation approved");
    public decimal ApprovedValue => Quotations
        .Where(entry => entry.Quotation.Status == "Approved")
        .Sum(entry => entry.Quotation.Amount);

    public IActionResult OnGet(int id)
    {
        var customer = customers.Find(id);

        if (customer is null)
        {
            return NotFound();
        }

        Customer = customer;
        Tab = Tab == "quotations" ? "quotations" : "requests";
        Requests = requests.ForCustomer(id);
        var requestIds = Requests.Select(request => request.Id).ToHashSet();
        Quotations = quotations.All.Where(entry => requestIds.Contains(entry.Quotation.RequestId)).ToArray();

        return Page();
    }
}
