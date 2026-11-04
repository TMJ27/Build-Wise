using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Areas.Configuration.Pages.Access;

public class IndexModel(AccessWorkspace access, CompanyService company) : PageModel
{
    public IReadOnlyList<AccessMember> Members { get; private set; } = [];
    public AccessMember Owner => Members.Single(member => member.Role == "Owner");
    public string CompanyName { get; private set; } = string.Empty;
    public int ActiveCount => Members.Count(member => member.IsActive);
    public int ApprovalCount => Members.Count(member => member.CanApprove);
    public bool AiConfigured => access.FindIntegration("ai")!.Version > 1;
    public bool EmailConfigured => access.FindIntegration("email")!.Version > 1;

    public async Task OnGetAsync()
    {
        Members = access.Members;
        CompanyName = (await company.GetCurrentAsync(HttpContext.RequestAborted))?.Name ?? string.Empty;
    }
}
