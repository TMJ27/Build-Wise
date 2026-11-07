using BuildWise.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.GeneratedNumbers;

public static class GeneratedNumberHelper
{
    // Rank the whole table independently of filters and pagination, as in Deporess.
    // The GUID breaks ties when creation dates are equal.
    private static async Task<Dictionary<Guid, int>> RanksAsync(IQueryable<Guid> orderedIds)
    {
        var ids = await orderedIds.ToListAsync();
        return ids.Select((id, index) => (id, sequence: index + 1))
            .ToDictionary(item => item.id, item => item.sequence);
    }

    private static async Task ApplyAsync<T>(
        IQueryable<Guid> orderedIds,
        IEnumerable<T> items,
        Func<T, Guid> getId,
        Action<T, int> assign)
    {
        var rows = items.ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var ranks = await RanksAsync(orderedIds);
        foreach (var row in rows)
        {
            assign(row, ranks.GetValueOrDefault(getId(row)));
        }
    }

    private static IQueryable<Guid> UserIds(BuildWiseDbContext db)
    {
        return db.Users.AsNoTracking()
            .Where(item => !db.CustomerAccounts.Any(account => account.UserId == item.Id))
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetUserSequenceAsync(this BuildWiseDbContext db, User item)
    {
        var ranks = await RanksAsync(UserIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyUserNumberAsync(this BuildWiseDbContext db, User item)
    {
        item.UserSequence = await db.GetUserSequenceAsync(item);
    }

    public static Task ApplyUserNumbersAsync(this BuildWiseDbContext db, IEnumerable<User> items)
    {
        return ApplyAsync(UserIds(db), items,
            item => item.Id, (item, sequence) => item.UserSequence = sequence);
    }

    private static IQueryable<Guid> UserGroupIds(BuildWiseDbContext db)
    {
        return db.UserGroups.AsNoTracking()
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetUserGroupSequenceAsync(this BuildWiseDbContext db, UserGroup item)
    {
        var ranks = await RanksAsync(UserGroupIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyUserGroupNumberAsync(this BuildWiseDbContext db, UserGroup item)
    {
        item.UserGroupSequence = await db.GetUserGroupSequenceAsync(item);
    }

    public static Task ApplyUserGroupNumbersAsync(this BuildWiseDbContext db, IEnumerable<UserGroup> items)
    {
        return ApplyAsync(UserGroupIds(db), items,
            item => item.Id, (item, sequence) => item.UserGroupSequence = sequence);
    }

    private static IQueryable<Guid> CurrencyIds(BuildWiseDbContext db)
    {
        return db.Currencies.AsNoTracking()
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetCurrencySequenceAsync(this BuildWiseDbContext db, Currency item)
    {
        var ranks = await RanksAsync(CurrencyIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyCurrencyNumberAsync(this BuildWiseDbContext db, Currency item)
    {
        item.CurrencySequence = await db.GetCurrencySequenceAsync(item);
    }

    public static Task ApplyCurrencyNumbersAsync(this BuildWiseDbContext db, IEnumerable<Currency> items)
    {
        return ApplyAsync(CurrencyIds(db), items,
            item => item.Id, (item, sequence) => item.CurrencySequence = sequence);
    }

    private static IQueryable<Guid> ServiceCategoryIds(BuildWiseDbContext db)
    {
        return db.ServiceCategories.AsNoTracking()
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetServiceCategorySequenceAsync(this BuildWiseDbContext db, ServiceCategory item)
    {
        var ranks = await RanksAsync(ServiceCategoryIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyServiceCategoryNumberAsync(this BuildWiseDbContext db, ServiceCategory item)
    {
        item.CategorySequence = await db.GetServiceCategorySequenceAsync(item);
    }

    public static Task ApplyServiceCategoryNumbersAsync(this BuildWiseDbContext db, IEnumerable<ServiceCategory> items)
    {
        return ApplyAsync(ServiceCategoryIds(db), items,
            item => item.Id, (item, sequence) => item.CategorySequence = sequence);
    }


    private static IQueryable<Guid> ClientIds(BuildWiseDbContext db)
    {
        return db.Clients.AsNoTracking()
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetClientSequenceAsync(this BuildWiseDbContext db, Client item)
    {
        var ranks = await RanksAsync(ClientIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyClientNumberAsync(this BuildWiseDbContext db, Client item)
    {
        item.ClientSequence = await db.GetClientSequenceAsync(item);
    }

    public static Task ApplyClientNumbersAsync(this BuildWiseDbContext db, IEnumerable<Client> items)
    {
        return ApplyAsync(ClientIds(db), items,
            item => item.Id, (item, sequence) => item.ClientSequence = sequence);
    }

    private static IQueryable<Guid> ServiceIds(BuildWiseDbContext db)
    {
        return db.Services.AsNoTracking()
            .OrderBy(item => item.CreationDate)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
    }

    public static async Task<int> GetServiceSequenceAsync(this BuildWiseDbContext db, Service item)
    {
        var ranks = await RanksAsync(ServiceIds(db));
        return ranks.GetValueOrDefault(item.Id);
    }

    public static async Task ApplyServiceNumberAsync(this BuildWiseDbContext db, Service item)
    {
        item.ServiceSequence = await db.GetServiceSequenceAsync(item);
    }

    public static Task ApplyServiceNumbersAsync(this BuildWiseDbContext db, IEnumerable<Service> items)
    {
        return ApplyAsync(ServiceIds(db), items,
            item => item.Id, (item, sequence) => item.ServiceSequence = sequence);
    }
}
