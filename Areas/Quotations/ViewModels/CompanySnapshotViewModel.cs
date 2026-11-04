using BuildWise.Data;

namespace BuildWise.Web.Areas.Quotations.ViewModels;

public sealed record CompanySnapshotViewModel(
    string Name,
    string Email,
    string Phone,
    string Address,
    string RegistrationNumber,
    string TaxNumber,
    string? LogoUrl,
    int DefaultValidityDays)
{
    public static CompanySnapshotViewModel FromCompany(Company company)
    {
        return new CompanySnapshotViewModel(
            company.Name,
            company.Email,
            company.Phone,
            company.Address,
            company.RegistrationNumber,
            company.TaxNumber,
            company.LogoStorageKey,
            company.DefaultValidityDays);
    }
}
