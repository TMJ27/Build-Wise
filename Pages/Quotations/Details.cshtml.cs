using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Quotations;

public class DetailsModel(QuotationWorkspace quotations, RequestWorkspace requests) : PageModel
{
    public QuotationEntry Entry { get; private set; } = null!;
    public RequestEntry SourceRequest { get; private set; } = null!;

    public IActionResult OnGet(int id)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        return Page();
    }

    public IActionResult OnPostApprove(int id, bool confirm)
    {
        if (!Load(id))
        {
            return NotFound();
        }

        if (!confirm)
        {
            ModelState.AddModelError(string.Empty, "Confirm that you have reviewed the scope, prices and terms.");
            return Page();
        }

        if (quotations.Approve(id))
        {
            requests.SetStatus(Entry.Quotation.RequestId, "Quotation approved");
            TempData["QuotationMessage"] = "Quotation approved. The approved document is ready to print.";
        }

        return RedirectToPage(new { id });
    }

    private bool Load(int id)
    {
        var entry = quotations.Find(id);
        var request = entry is null ? null : requests.Find(entry.Quotation.RequestId);

        if (entry is null || request is null)
        {
            return false;
        }

        Entry = entry;
        SourceRequest = request;
        return true;
    }
}
