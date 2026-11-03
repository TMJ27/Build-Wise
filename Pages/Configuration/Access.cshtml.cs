using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BuildWise.Web.Pages.Configuration;

public class AccessModel(AccessWorkspace access, CompanyWorkspace company) : PageModel
{
    public IReadOnlyList<AccessMember> Members { get; private set; } = [];
    public AccessMember Owner => Members.Single(member => member.Role == "Owner");
    public string CompanyName => company.Current.Name;
    public int ActiveCount => Members.Count(member => member.IsActive);
    public int ApprovalCount => Members.Count(member => member.CanApprove);
    public bool AiConfigured => access.FindIntegration("ai")!.Version > 1;
    public bool EmailConfigured => access.FindIntegration("email")!.Version > 1;

    public void OnGet()
    {
        Members = access.Members;
    }
}
