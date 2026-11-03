using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class Company
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string RegistrationNumber { get; set; } = null!;

    public string TaxNumber { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? LogoStorageKey { get; set; }

    public string DefaultCurrency { get; set; } = null!;

    public string? DefaultQuotationTerms { get; set; }

    public int DefaultValidityDays { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdated { get; set; }
}
