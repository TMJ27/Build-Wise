using BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.Inclusions;

public class UpsertModel : PageModel
{
    [BindProperty]
    public InclusionUpsertViewModel Input { get; set; } = new();

    public void OnGet(Guid? id)
    {
        Input.Id = id;
        // TODO: Load the record for editing. Add save and validation handlers when implementing the backend.
    }
}
