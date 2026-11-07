using BuildWise.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BuildWise.Lib.Enums;
using BuildWise.Lib.Enums.Statuses;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Areas.Customers.Pages;

public class IndexModel(BuildWiseDbContext Db) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Type { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    public int PageSize => 10;
    public int FilteredCount { get; private set; }
    public int PageCount { get; private set; } = 1;

    public int TotalCount { get; private set; }
    public int IndividualsCount { get; private set; }
    public int BusinessCount { get; private set; }
    public int OpenCount { get; private set; }

    public IReadOnlyList<Client> ClientList { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        Search = (Search ?? string.Empty).Trim();
        if (Search.Length > 120)
        {
            Search = Search[..120];
        }

        TotalCount = await Db.Clients.CountAsync();
        BusinessCount = await Db.Clients.CountAsync(client => client.ClientTypeId == (int)ClientType.Business);
        IndividualsCount = await Db.Clients.CountAsync(client => client.ClientTypeId == (int)ClientType.Individual);
        OpenCount = await Db.Clients.CountAsync(client =>
            client.ClientRequests.Any(request => request.StatusId == (int)ClientRequestStatus.Open));

        var query = Db.Clients.AsNoTracking();

        if (Enum.TryParse<ClientType>(Type, true, out var clientType) && Enum.IsDefined(clientType))
        {
            query = query.Where(client => client.ClientTypeId == (int)clientType);
            Type = ((int)clientType).ToString();
        }
        else
        {
            Type = "all";
        }

        if (Search.Length > 0)
        {
            var searchText = Search.ToUpper();
            query = query.Where(client => client.Name.ToUpper().Contains(searchText)
                || client.Email.ToUpper().Contains(searchText)
                || (client.Phone != null && client.Phone.ToUpper().Contains(searchText))
                || client.Address.ToUpper().Contains(searchText));
        }

        Sort = Sort is "newest" or "oldest" ? Sort : "name";
        FilteredCount = await query.CountAsync();
        PageCount = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);

        query = Sort switch
        {
            "newest" => query.OrderByDescending(client => client.CreationDate).ThenBy(client => client.Id),
            "oldest" => query.OrderBy(client => client.CreationDate).ThenBy(client => client.Id),
            _ => query.OrderBy(client => client.Name).ThenBy(client => client.Id)
        };

        ClientList = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        await Db.ApplyClientNumbersAsync(ClientList);

        return Page();
    }
}
