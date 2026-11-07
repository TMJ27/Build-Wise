using BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.Inclusions;

public class DetailsModel : PageModel
{
    public InclusionDetailsViewModel InclusionDetails { get; set; } = new();

    public void OnGet(Guid id)
    {
        InclusionDetails.Id = id;
        // TODO: Load and map the record for this GUID; return NotFound when it does not exist.
    }
}
