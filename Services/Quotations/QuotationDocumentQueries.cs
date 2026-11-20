using BuildWise.Data;
using BuildWise.Web.Areas.Quotations.ViewModels;
using BuildWise.Web.Services.GeneratedNumbers;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Web.Services.Quotations;

public static class QuotationDocumentQueries
{
    public static async Task<QuotationDocumentViewModel?> LoadAsync(BuildWiseDbContext db, Guid id, Guid? revisionId = null)
    {
        var quotation = await db.Quotations.AsNoTracking().Include(item => item.Request)
            .SingleOrDefaultAsync(item => item.Id == id);
        if (quotation is null) return null;
        var revisions = await db.QuotationRevisions.AsNoTracking().Include(item => item.Creator)
            .Include(item => item.ApprovedByUser).Include(item => item.QuotationItems)
            .Where(item => item.QuotationId == id).OrderByDescending(item => item.RevisionNumber).ToListAsync();
        var revision = revisionId.HasValue ? revisions.SingleOrDefault(item => item.Id == revisionId) : revisions.FirstOrDefault();
        if (revision is null) return null;
        await db.ApplyQuotationNumberAsync(quotation);
        var logo = revision.CompanyLogoStorageKeySnapshot;
        if (string.IsNullOrWhiteSpace(logo))
            logo = await db.Companies.AsNoTracking().OrderBy(company => company.CreationDate)
                .Select(company => company.LogoStorageKey).FirstOrDefaultAsync();
        return new QuotationDocumentViewModel
        {
            Id = id, Number = quotation.GeneratedQuotationNumber, RequestReference = quotation.Request.ReferenceNumber,
            Revision = revision, Revisions = revisions, LogoUrl = logo
        };
    }
}
