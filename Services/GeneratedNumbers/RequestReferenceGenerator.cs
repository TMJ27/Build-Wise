using System.Data;
using System.Globalization;
using BuildWise.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BuildWise.Web.Services.GeneratedNumbers;

public static class RequestReferenceGenerator
{
    public static async Task<string> GenerateAsync(BuildWiseDbContext db, DateTime createdAt)
    {
        var prefix = $"REQ-{createdAt:yyyyMMdd}-";
        var references = await db.ClientRequests.AsNoTracking()
            .Where(request => request.ReferenceNumber != null && request.ReferenceNumber.StartsWith(prefix))
            .Select(request => request.ReferenceNumber!)
            .ToListAsync();

        var lastSequence = references
            .Select(reference => reference[prefix.Length..])
            .Select(suffix => int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
                ? sequence : 0)
            .DefaultIfEmpty()
            .Max();

        return GeneratedNumberFormat.Format("REQ", createdAt, lastSequence + 1);
    }

    public static async Task<IDbContextTransaction?> BeginCreationTransactionAsync(BuildWiseDbContext db)
    {
        return db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;
    }
}
