using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Quotations;

public class PreviewModel(QuotationWorkspace quotations, RequestWorkspace requests) : PageModel
{
    public QuotationEntry Entry { get; private set; } = null!;
    public RequestEntry SourceRequest { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        var entry = quotations.Find(id);
        var request = entry is null ? null : requests.Find(entry.Quotation.RequestId);

        if (entry is null || request is null)
        {
            return NotFound();
        }

        Entry = entry;
        SourceRequest = request;
        return Page();
    }
}
