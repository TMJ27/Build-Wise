using BuildWise.Data;
using BuildWise.Web.Areas.Requests.ViewModels;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.Scopes;
using BuildWise.Lib.Enums.Statuses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Requests.Pages;

public class DetailsModel(BuildWiseDbContext db) : PageModel
{
    public RequestDetailsViewModel RequestDetails { get; private set; } = new();
    public IReadOnlyList<QuotationIndexViewModel> Quotations { get; private set; } = [];
    public bool CanPrepareScope => ScopeQueries.CanEdit(RequestDetails.StatusId);

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var request = await db.ClientRequests.AsNoTracking().Include(request => request.Client)
            .Include(request => request.Creator).SingleOrDefaultAsync(request => request.Id == id);
        if (request is null) return NotFound();
        RequestDetails = new RequestDetailsViewModel
        {
            Id = request.Id, ClientId = request.ClientId, ReferenceNumber = request.ReferenceNumber,
            Title = request.Title, CustomerName = request.Client.Name, CustomerEmail = request.Client.Email,
            CustomerPhone = request.Client.Phone, SiteAddress = request.SiteAddress, PropertyType = request.PropertyType,
            Description = request.Description, StatusId = request.StatusId, CreationDate = request.CreationDate,
            ScopeItemCount = await db.ScopeItems.CountAsync(item => item.RequestId == request.Id),
            CreatorName = string.Join(" ", new[] { request.Creator.Name, request.Creator.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value))) is { Length: > 0 } name ? name : request.Creator.Email
        };
        Quotations = await db.Quotations.AsNoTracking().Where(quotation => quotation.RequestId == id)
            .OrderBy(quotation => quotation.QuotationNumber).Select(quotation => new
            {
                Quotation = quotation,
                Revision = quotation.QuotationRevisions.OrderByDescending(revision => revision.RevisionNumber).FirstOrDefault()
            }).Select(row => new QuotationIndexViewModel
            {
                Id = row.Quotation.Id, RequestId = row.Quotation.RequestId, QuotationNumber = row.Quotation.QuotationNumber,
                Title = row.Revision == null ? row.Quotation.Request.Title : row.Revision.Title,
                RevisionNumber = row.Revision == null ? 0 : row.Revision.RevisionNumber,
                Status = row.Revision == null ? null : (QuotationStatus?)row.Revision.StatusId,
                IncludesPrices = row.Revision != null && row.Revision.IncludesPrices,
                CurrencyCode = row.Revision == null ? null : row.Revision.CurrencyCode,
                Total = row.Revision == null ? null : row.Revision.Total
            }).ToListAsync();
        return Page();
    }
}
