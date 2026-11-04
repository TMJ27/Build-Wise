using System.ComponentModel.DataAnnotations;

namespace BuildWise.Web.Areas.Configuration.ViewModels.Profile;

public sealed class ProfileInputViewModel
{
    public Guid CompanyId { get; set; }

    [Required, StringLength(30)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [StringLength(50)]
    public string? RegistrationNumber { get; set; }

    [StringLength(50)]
    public string? TaxNumber { get; set; }

    [Range(0, 365)]
    public int DefaultValidityDays { get; set; }
    public string? DefaultQuotationTerms { get; set; }
    public string? DefaultCurrency { get; set; }
    public string? DefaultTerms { get; set; } 

}
