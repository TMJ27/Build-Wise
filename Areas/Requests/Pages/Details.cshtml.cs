using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Requests.Pages;

public class DetailsModel(RequestWorkspace workspace, QuotationWorkspace quotations) : PageModel
{
    public RequestEntry Entry { get; private set; } = null!;
    public QuotationEntry? Quotation { get; private set; }

    public IActionResult OnGet(int id)
    {
        var entry = workspace.Find(id);

        if (entry is null)
        {
            return NotFound();
        }

        Entry = entry;
        Quotation = quotations.All.FirstOrDefault(quotation => quotation.Quotation.RequestId == id);
        return Page();
    }
}
