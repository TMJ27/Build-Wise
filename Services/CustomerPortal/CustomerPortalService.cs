using System.Security.Claims;
using BuildWise.Data;
using BuildWise.Lib.Enums.Statuses;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.CustomerPortal;

public sealed class CustomerPortalService(BuildWiseDbContext db)
{
    public async Task<CustomerAccount?> GetAccountAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identities.FirstOrDefault(identity => identity.IsAuthenticated
            && identity.AuthenticationType == CustomerPortalAuthentication.Scheme);
        if (identity is null || !Guid.TryParse(identity.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)) return null;
        var stamp = identity.FindFirst(CustomerPortalAuthentication.StampClaim)?.Value;
        if (string.IsNullOrWhiteSpace(stamp)) return null;
        return await db.CustomerAccounts.AsNoTracking().Include(account => account.Client)
            .SingleOrDefaultAsync(account => account.Id == id && account.SecurityStamp == stamp && account.IsActive && account.Client.IsActive);
    }

    public IQueryable<ClientRequest> Requests(CustomerAccount account) => db.ClientRequests.AsNoTracking()
        .Where(request => request.ClientId == account.ClientId);

    // Only the latest approved revision is customer-visible; never expose drafts or another customer's data.
    public IQueryable<QuotationRevision> Quotations(CustomerAccount account) => db.QuotationRevisions.AsNoTracking()
        .Where(revision => revision.Quotation.Request.ClientId == account.ClientId
            && revision.StatusId == (int)QuotationStatus.Approved && revision.ApprovalDate != null
            && !db.QuotationRevisions.Any(newer => newer.QuotationId == revision.QuotationId
                && newer.RevisionNumber > revision.RevisionNumber && newer.StatusId == (int)QuotationStatus.Approved
                && newer.ApprovalDate != null));
}
