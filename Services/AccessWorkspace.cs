namespace BuildWise.Web.Services;

// UI sample configuration. Authentication and external connections are not changed here.
public sealed class AccessWorkspace
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, AccessMember> members = new();
    private readonly Dictionary<string, IntegrationPreferences> integrations = new(StringComparer.Ordinal);

    public static IReadOnlyList<string> EditableRoles { get; } = Array.AsReadOnly(new[]
    {
        "Administrator", "Estimator", "Viewer"
    });

    public AccessWorkspace()
    {
        var initialMembers = new[]
        {
            new AccessMember(MemberId(1), "Tawanda Moyo", "tawanda@mavambo.example", "Owner", true, 1),
            new AccessMember(MemberId(2), "Ruth Chigumba", "ruth@mavambo.example", "Administrator", true, 1),
            new AccessMember(MemberId(3), "Tatenda Ndlovu", "tatenda@mavambo.example", "Estimator", true, 1),
            new AccessMember(MemberId(4), "Grace Moyo", "grace@mavambo.example", "Viewer", false, 1)
        };

        foreach (var member in initialMembers)
        {
            members[member.Id] = member;
        }

        integrations["ai"] = new("ai", "OpenAI", "", "", "", "", 587, true, false, 1);
        integrations["email"] = new("email", "SMTP", "", "Mavambo Projects", "", "", 587, true, false, 1);
    }

    public IReadOnlyList<AccessMember> Members
    {
        get
        {
            lock (sync)
            {
                return members.Values.OrderBy(member => member.Role == "Owner" ? 0 : 1)
                    .ThenBy(member => member.Name).ToArray();
            }
        }
    }

    public AccessMember? FindMember(Guid id)
    {
        lock (sync)
        {
            return members.GetValueOrDefault(id);
        }
    }

    public AccessSaveResult SaveMember(Guid? id, string name, string email, string role, bool active, long version)
    {
        lock (sync)
        {
            var existing = id.HasValue ? members.GetValueOrDefault(id.Value) : null;

            if (id.HasValue && (existing is null || existing.Role == "Owner"))
            {
                return new(null, "The workspace owner cannot be changed from this form.");
            }

            if (existing is not null && existing.Version != version)
            {
                return new(null, "This member changed in another window. Reload before saving again.");
            }

            if (!EditableRoles.Contains(role))
            {
                return new(null, "Choose a valid team role.");
            }

            if (members.Values.Any(member => member.Id != id &&
                member.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return new(null, "A team member with this email address already exists.");
            }

            var memberId = id ?? Guid.NewGuid();
            members[memberId] = new(memberId, name.Trim(), email.Trim(), role, active, (existing?.Version ?? 0) + 1);
            return new(memberId, null);
        }
    }

    public IntegrationPreferences? FindIntegration(string kind)
    {
        lock (sync)
        {
            return integrations.GetValueOrDefault(kind);
        }
    }

    public bool SaveIntegration(IntegrationPreferences preferences, long expectedVersion)
    {
        lock (sync)
        {
            if (!integrations.TryGetValue(preferences.Kind, out var existing) || existing.Version != expectedVersion)
            {
                return false;
            }

            integrations[preferences.Kind] = preferences with { Version = existing.Version + 1 };
            return true;
        }
    }

    private static Guid MemberId(int number) => Guid.Parse($"b17d0000-0000-4000-9000-{number:D12}");
}

public sealed record AccessMember(Guid Id, string Name, string Email, string Role, bool IsActive, long Version)
{
    public bool CanApprove => IsActive && (Role is "Owner" or "Administrator");

    public string Initials => string.Concat(Name
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(part => char.ToUpperInvariant(part[0])));
}

public sealed record IntegrationPreferences(
    string Kind,
    string Provider,
    string Model,
    string SenderName,
    string SenderEmail,
    string Host,
    int Port,
    bool UseTls,
    bool EnableWhenConnected,
    long Version);

public sealed record AccessSaveResult(Guid? Id, string? Error);
