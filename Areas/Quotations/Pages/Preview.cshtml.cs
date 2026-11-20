using BuildWise.Data;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.Quotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class PreviewModel(BuildWiseDbContext db) : PageModel
{
    public QuotationDocumentViewModel Document { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, Guid? revisionId)
    {
        var document = await QuotationDocumentQueries.LoadAsync(db, id, revisionId);
        if (document is null) return NotFound();
        Document = document;
        return Page();
    }

}
