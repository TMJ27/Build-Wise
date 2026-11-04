using BuildWise.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Web.ViewModels.Dashboard;
using BuildWise.Web.Areas.Quotations.ViewModels;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class EditModel(
    QuotationWorkspace quotations,
    RequestWorkspace requests,
    CompanyService company) : PageModel
{
    [BindProperty]
    public QuotationInputViewModel Input { get; set; } = new();

    public QuotationEntry? Existing { get; private set; }
    public IReadOnlyList<DashboardRequest> AvailableRequests { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int? id, int? requestId)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (Existing is not null)
        {
            Input = new QuotationInputViewModel
            {
                RequestId = Existing.Quotation.RequestId,
                Terms = Existing.Terms,
                Exclusions = Existing.Exclusions,
                Lines = Existing.Lines.Select(line => new LineInputViewModel
                {
                    Description = line.Description,
                    Quantity = line.Quantity,
                    Rate = line.Rate
                }).ToList()
            };
        }
        else
        {
            var profile = await company.GetCurrentAsync(HttpContext.RequestAborted);
            Input.Terms = profile?.DefaultQuotationTerms ?? string.Empty;

            if (AvailableRequests.Any(request => request.Id == requestId))
            {
                Input.RequestId = requestId!.Value;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (!AvailableRequests.Any(request => request.Id == Input.RequestId) ||
            (Existing is not null && Input.RequestId != Existing.Quotation.RequestId))
        {
            ModelState.AddModelError("Input.RequestId", "Select an eligible request.");
        }

        if (Input.Lines.Count is < 1 or > 30)
        {
            ModelState.AddModelError(string.Empty, "Include between 1 and 30 pricing items.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var profile = await company.GetCurrentAsync(HttpContext.RequestAborted);

        if (profile is null)
        {
            ModelState.AddModelError(string.Empty, "Save the business profile before creating a quotation.");
            return Page();
        }

        var lines = Input.Lines.Select(line => new QuotationLine(
            line.Description.Trim(), line.Quantity, line.Rate)).ToArray();
        var quotationId = quotations.Save(
            id, Input.RequestId, lines, Input.Terms, Input.Exclusions,
            CompanySnapshotViewModel.FromCompany(profile));

        if (!quotationId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "This quotation has changed. Reload the page before saving.");
            return Page();
        }

        requests.SetStatus(Input.RequestId, "Draft quotation");
        TempData["QuotationMessage"] = "Draft saved. Review the document before approval.";
        return RedirectToPage("Details", new { id = quotationId });
    }

    public IActionResult OnPostAddLine(int? id)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (Input.Lines.Count < 30)
        {
            Input.Lines.Add(new LineInputViewModel());
        }

        ModelState.Clear();
        return Page();
    }

    public IActionResult OnPostRemoveLine(int? id, int lineIndex)
    {
        var result = Load(id);

        if (result is not null)
        {
            return result;
        }

        if (Input.Lines.Count > 1 && lineIndex >= 0 && lineIndex < Input.Lines.Count)
        {
            Input.Lines.RemoveAt(lineIndex);
        }

        ModelState.Clear();
        return Page();
    }

    private IActionResult? Load(int? id)
    {
        Existing = id.HasValue ? quotations.Find(id.Value) : null;

        if (id.HasValue && Existing is null)
        {
            return NotFound();
        }

        if (Existing?.Quotation.Status == "Approved")
        {
            return RedirectToPage("Details", new { id });
        }

        var quotedRequestIds = quotations.All.Select(entry => entry.Quotation.RequestId).ToHashSet();
        AvailableRequests = requests.Requests.Where(request =>
            request.Id == Existing?.Quotation.RequestId ||
            (request.Status == "Ready to quote" && !quotedRequestIds.Contains(request.Id)))
            .ToArray();

        return null;
    }

}
