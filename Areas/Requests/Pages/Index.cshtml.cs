using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Areas.Requests.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Requests.Pages;

public class IndexModel(BuildWiseDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Search { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public ClientRequestStatus? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "newest";
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public int PageSize => 10;
    public int TotalCount { get; private set; }
    public int FilteredCount { get; private set; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    public IReadOnlyList<RequestIndexViewModel> Records { get; private set; } = [];
    public Dictionary<int, int> StatusCounts { get; private set; } = [];
    public int Count(ClientRequestStatus? status) => status.HasValue ? StatusCounts.GetValueOrDefault((int)status.Value) : TotalCount;

    public async Task OnGetAsync()
    {
        Search = (Search ?? string.Empty).Trim();
        if (Search.Length > 120) Search = Search[..120];
        if ((Status.HasValue && !Enum.IsDefined(Status.Value)) || ModelState[nameof(Status)]?.Errors.Count > 0)
        {
            Status = null;
            ModelState.Remove(nameof(Status));
        }
        Sort = Sort is "oldest" or "customer" ? Sort : "newest";
        StatusCounts = await db.ClientRequests.AsNoTracking().GroupBy(request => request.StatusId)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count);
        TotalCount = StatusCounts.Values.Sum();
        var query = db.ClientRequests.AsNoTracking();
        if (Status.HasValue) query = query.Where(request => request.StatusId == (int)Status.Value);
        if (Search.Length > 0)
        {
            var term = Search.ToUpper();
            query = query.Where(request => request.Title.ToUpper().Contains(term)
                || request.Client.Name.ToUpper().Contains(term) || request.SiteAddress.ToUpper().Contains(term)
                || (request.ReferenceNumber != null && request.ReferenceNumber.ToUpper().Contains(term)));
        }
        FilteredCount = await query.CountAsync();
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
        query = Sort switch
        {
            "oldest" => query.OrderBy(request => request.CreationDate).ThenBy(request => request.Id),
            "customer" => query.OrderBy(request => request.Client.Name).ThenBy(request => request.Id),
            _ => query.OrderByDescending(request => request.CreationDate).ThenBy(request => request.Id)
        };
        Records = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).Select(request => new RequestIndexViewModel
        {
            Id = request.Id, ReferenceNumber = request.ReferenceNumber, Title = request.Title,
            ClientId = request.ClientId, CustomerName = request.Client.Name, SiteAddress = request.SiteAddress,
            StatusId = request.StatusId, CreationDate = request.CreationDate, ScopeItemCount = request.ScopeItems.Count
        }).ToListAsync();
    }
}
