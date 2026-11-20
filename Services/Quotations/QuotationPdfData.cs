using System.Text.Json;
using BuildWise.Data;
using BuildWise.Web.Areas.Quotations.ViewModels;

namespace BuildWise.Web.Services.Quotations;

public static class QuotationPdfData
{
    public static object Create(QuotationDocumentViewModel document)
    {
        var revision = document.Revision;
        if (!string.IsNullOrWhiteSpace(revision.ApprovedDocumentJson))
        {
            using var snapshot = JsonDocument.Parse(revision.ApprovedDocumentJson);
            revision = JsonSerializer.Deserialize<QuotationRevision>(revision.ApprovedDocumentJson)
                ?? throw new InvalidOperationException("The approved document could not be read.");
            revision.QuotationItems = snapshot.RootElement.GetProperty("Items").Deserialize<List<QuotationItem>>() ?? [];
        }
        return new
        {
            document.Number, document.RequestReference,
            LogoUrl = revision.CompanyLogoStorageKeySnapshot ?? document.LogoUrl,
            revision.RevisionNumber, revision.Title, revision.CompanyNameSnapshot,
            revision.CompanyAddressSnapshot, revision.CompanyEmailSnapshot, revision.CompanyPhoneSnapshot,
            revision.CompanyRegistrationNumberSnapshot, revision.CompanyTaxNumberSnapshot,
            revision.CustomerNameSnapshot, revision.CustomerEmailSnapshot, revision.CustomerPhoneSnapshot,
            revision.CustomerAddressSnapshot, revision.SiteAddressSnapshot, revision.IssueDate, revision.ValidUntil,
            revision.IncludesPrices, revision.CurrencyCode, revision.Subtotal, revision.TaxAmount, revision.Total,
            revision.Terms, revision.Exclusions,
            Items = revision.QuotationItems.OrderBy(item => item.Title).Select(item => new
            {
                item.Title, item.Description, item.Quantity, item.UnitCodeSnapshot, item.LineAmount
            })
        };
    }
}
