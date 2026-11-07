using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Quotations.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Quotations.Pages;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public QuotationStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "newest";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int PageSize => 10;
    public int FilteredCount { get; set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    public QuotationSummaryViewModel Summary { get; set; } = new();
    public List<QuotationIndexViewModel> Quotations { get; set; } = [];

    public async Task OnGetAsync()
    {
        Search = (Search ?? string.Empty).Trim();
        if (Search.Length > 120)
        {
            Search = Search[..120];
        }
        if (Status.HasValue && !Enum.IsDefined(Status.Value))
        {
            Status = null;
            ModelState.Remove(nameof(Status));
        }
        Sort = Sort is "oldest" or "customer" ? Sort : "newest";

        // Keep one row per quotation, including quotations without a revision yet.
        // Amounts and status belong to a specific revision, never to the request.
        var query = db.Quotations
            .AsNoTracking()
            .Select(quotation => new
            {
                Quotation = quotation,
                Revision = quotation.QuotationRevisions
                    .OrderByDescending(revision => revision.RevisionNumber)
                    .FirstOrDefault()
            })
            .Select(record => new QuotationIndexViewModel
            {
                Id = record.Quotation.Id,
                RequestId = record.Quotation.RequestId,
                QuotationNumber = record.Quotation.QuotationNumber,
                Title = record.Revision == null ? record.Quotation.Request.Title : record.Revision.Title,
                CustomerName = record.Revision == null ? record.Quotation.Request.Client.Name : record.Revision.CustomerNameSnapshot,
                RevisionNumber = record.Revision == null ? 0 : record.Revision.RevisionNumber,
                Status = record.Revision == null ? null : (QuotationStatus?)record.Revision.StatusId,
                IncludesPrices = record.Revision != null && record.Revision.IncludesPrices,
                CurrencyCode = record.Revision == null ? null : record.Revision.CurrencyCode,
                Total = record.Revision == null ? null : record.Revision.Total,
                CreationDate = record.Revision == null ? null : record.Revision.CreationDate,
                CreatorName = ((record.Quotation.Creator.Name ?? "") + " " + (record.Quotation.Creator.Surname ?? "")).Trim() == ""
                    ? record.Quotation.Creator.Email
                    : ((record.Quotation.Creator.Name ?? "") + " " + (record.Quotation.Creator.Surname ?? "")).Trim()
            });

        Summary = new QuotationSummaryViewModel
        {
            TotalCount = await query.CountAsync(),
            DraftCount = await query.CountAsync(quotation => quotation.Status == QuotationStatus.Draft),
            ApprovedCount = await query.CountAsync(quotation => quotation.Status == QuotationStatus.Approved),
            PricedCount = await query.CountAsync(quotation => quotation.IncludesPrices && quotation.Total.HasValue)
        };

        if (Status.HasValue)
        {
            query = query.Where(quotation => quotation.Status == Status);
        }
        if (Search.Length > 0)
        {
            var searchText = Search.ToUpper();
            query = query.Where(quotation => quotation.QuotationNumber.ToUpper().Contains(searchText)
                || quotation.Title.ToUpper().Contains(searchText)
                || quotation.CustomerName.ToUpper().Contains(searchText));
        }

        FilteredCount = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        query = Sort switch
        {
            "oldest" => query.OrderBy(quotation => quotation.CreationDate).ThenBy(quotation => quotation.Id),
            "customer" => query.OrderBy(quotation => quotation.CustomerName).ThenBy(quotation => quotation.Id),
            _ => query.OrderByDescending(quotation => quotation.CreationDate).ThenBy(quotation => quotation.Id)
        };
        Quotations = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
    }
}
