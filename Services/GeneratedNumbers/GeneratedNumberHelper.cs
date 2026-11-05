using BuildWise.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.GeneratedNumbers;

public static class GeneratedNumberHelper
{
    // Rank the complete table before applying numbers to any filtered or paged list.
    // Creation date determines the order; the GUID breaks timestamp ties.
    private static async Task ApplyAsync<T>(
        IQueryable<Guid> orderedIds,
        IEnumerable<T> items,
        Func<T, Guid> getId,
        Action<T, int> assign)
    {
        var rows = items.ToArray();
        if (rows.Length == 0)
        {
            return;
        }

        var ids = await orderedIds.ToListAsync();
        var sequences = ids.Select((id, index) => (id, sequence: index + 1))
            .ToDictionary(item => item.id, item => item.sequence);

        foreach (var row in rows)
        {
            if (!sequences.TryGetValue(getId(row), out var sequence))
            {
                throw new InvalidOperationException("Cannot number a record that is not in the database.");
            }

            assign(row, sequence);
        }
    }

    public static Task ApplyUserNumbersAsync(this BuildWiseDbContext db, IEnumerable<User> users)
    {
        return ApplyAsync(
            db.Users.AsNoTracking().OrderBy(user => user.CreationDate).ThenBy(user => user.Id).Select(user => user.Id),
            users, user => user.Id, (user, sequence) => user.UserSequence = sequence);
    }

    public static Task ApplyUserNumberAsync(this BuildWiseDbContext db, User user)
    {
        return db.ApplyUserNumbersAsync([user]);
    }

    public static Task ApplyUserGroupNumbersAsync(this BuildWiseDbContext db, IEnumerable<UserGroup> groups)
    {
        return ApplyAsync(
            db.UserGroups.AsNoTracking().OrderBy(group => group.CreationDate).ThenBy(group => group.Id).Select(group => group.Id),
            groups, group => group.Id, (group, sequence) => group.UserGroupSequence = sequence);
    }

    public static Task ApplyUserGroupNumberAsync(this BuildWiseDbContext db, UserGroup group)
    {
        return db.ApplyUserGroupNumbersAsync([group]);
    }

    public static Task ApplyCurrencyNumbersAsync(this BuildWiseDbContext db, IEnumerable<Currency> currencies)
    {
        return ApplyAsync(
            db.Currencies.AsNoTracking().OrderBy(currency => currency.CreationDate).ThenBy(currency => currency.Id).Select(currency => currency.Id),
            currencies, currency => currency.Id, (currency, sequence) => currency.CurrencySequence = sequence);
    }

    public static Task ApplyCurrencyNumberAsync(this BuildWiseDbContext db, Currency currency)
    {
        return db.ApplyCurrencyNumbersAsync([currency]);
    }
}
