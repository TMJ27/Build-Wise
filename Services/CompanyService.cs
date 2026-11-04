using BuildWise.Data;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services;

public sealed class CompanyService(BuildWiseDbContext context)
{
    public Task<Company?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        // Each deployment uses its own database and has one company.
        return context.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }
}
