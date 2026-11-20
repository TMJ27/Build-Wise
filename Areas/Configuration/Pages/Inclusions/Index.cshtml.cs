using BuildWise.Web.Areas.Configuration.ViewModels.Inclusions;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.Inclusions;

public class IndexModel : PageModel
{
    // TODO: Load configuration records here when implementing the backend.
    public IReadOnlyList<InclusionRowViewModel> Records { get; set; } = [];
}
