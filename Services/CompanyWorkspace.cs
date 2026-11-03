namespace BuildWise.Web.Services;

// Single-company sample configuration used by the native design pages.
public sealed class CompanyWorkspace
{
    private readonly object sync = new();
    private CompanyProfile profile = new(
        Guid.Parse("b17d0000-0000-4000-8000-000000000100"),
        "Mavambo Projects",
        "hello@mavambo.example",
        "+263 77 123 4567",
        "Harare, Zimbabwe",
        string.Empty,
        string.Empty,
        null,
        14,
        "Valid for 14 days. Work starts after site access and payment arrangements are confirmed.",
        "Additional work outside the agreed scope will be quoted separately.",
        1);

    public CompanyWorkspace()
    {
        Initial = profile;
    }

    public CompanyProfile Initial { get; }

    public CompanyProfile Current
    {
        get
        {
            lock (sync)
            {
                return profile;
            }
        }
    }

    public bool Save(CompanyProfile updated, long expectedVersion)
    {
        lock (sync)
        {
            if (profile.Version != expectedVersion)
            {
                return false;
            }

            profile = updated with { Id = profile.Id, Version = profile.Version + 1 };
            return true;
        }
    }
}

public sealed record CompanyProfile(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Address,
    string RegistrationNumber,
    string TaxNumber,
    string? LogoDataUrl,
    int DefaultValidityDays,
    string DefaultTerms,
    string DefaultExclusions,
    long Version)
{
    public string Initials => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
}
