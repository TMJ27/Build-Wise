namespace BuildWise.Web.Services;

// Sample catalogue for the design workflow. Database integration is a separate step.
public sealed class CatalogueWorkspace
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, CatalogueService> services;

    public static IReadOnlyList<string> Categories { get; } = Array.AsReadOnly(new[]
    {
        "Electrical", "Painting", "Plumbing", "Renovations", "Maintenance"
    });

    public static IReadOnlyList<string> Units { get; } = Array.AsReadOnly(new[]
    {
        "Item", "Hour", "Metre", "Square metre", "Project"
    });

    public CatalogueWorkspace()
    {
        var records = new[]
        {
            Seed(1, "ELE-001", "Electrical installation", "Electrical", "Item", true,
                "Internal wiring, sockets and lighting for new and existing properties.",
                ["Internal wiring and agreed fittings", "Testing of the completed installation"],
                ["External supply connection", "Repairs outside the agreed work areas"],
                ["Which rooms or areas need work?", "Does wiring already exist?", "Are drawings available?"],
                ["Site assessment", "Wiring and fittings", "Testing and handover"], [ServiceId(5)]),
            Seed(2, "PNT-001", "Interior & exterior painting", "Painting", "Square metre", false,
                "Surface preparation and paint finishes for walls, ceilings and exterior surfaces.",
                ["Preparation of agreed surfaces", "Application of the specified paint finish"],
                ["Structural repairs", "Specialist damp treatment unless included in the scope"],
                ["Which surfaces need painting?", "What is the current surface condition?", "Which finish is preferred?"],
                ["Surface preparation", "Primer and painting", "Final inspection"], []),
            Seed(3, "PLM-001", "Plumbing & repairs", "Plumbing", "Item", true,
                "Pipework, fittings and repairs for leaking or damaged plumbing installations.",
                ["Inspection of the affected plumbing", "Agreed repairs and leak checks"],
                ["Municipal supply work", "Concealed damage beyond the inspected area"],
                ["Where is the plumbing issue?", "Is the supply currently isolated?", "Are replacement fittings available?"],
                ["Inspect and isolate", "Repair or replace", "Pressure and leak checks"], []),
            Seed(4, "REN-001", "Property renovations", "Renovations", "Project", true,
                "Coordinated improvements to existing rooms and spaces, shaped around an agreed scope.",
                ["Review of the proposed changes", "Agreed renovation activities"],
                ["Unspecified structural changes", "Approvals and specialist work unless agreed"],
                ["Which areas are being renovated?", "What changes are required?", "Will the property remain occupied?"],
                ["Assessment and planning", "Preparation and works", "Finishes and handover"], []),
            Seed(5, "ELE-002", "Electrical site assessment", "Electrical", "Project", true,
                "An inspection to establish installation conditions and the information needed to plan electrical work.",
                ["Inspection of accessible work areas", "Recorded findings and assessment notes"],
                ["Installation and repair work", "Opening concealed areas unless agreed"],
                ["What prompted the assessment?", "Which areas can be accessed?"],
                ["Site visit", "Review findings", "Confirm next steps"], []),
            Seed(6, "MNT-001", "General maintenance", "Maintenance", "Hour", false,
                "Routine repairs and maintenance tasks for homes, offices and small commercial properties.",
                ["Agreed minor repairs", "Work-area clean-up"],
                ["Specialist installation work", "Materials not included in the agreed quotation"],
                ["What needs attention?", "Are there any access restrictions?"],
                ["Confirm tasks", "Complete maintenance", "Review completed work"], []),
            Seed(7, "MNT-002", "Roof inspection", "Maintenance", "Project", true,
                "Visual inspection of accessible roof areas and a summary of visible maintenance concerns.",
                ["Visual inspection", "Recorded observations"],
                ["Roof repairs", "Specialist access equipment"],
                ["What issue has been observed?", "Is safe roof access available?"],
                ["Assess access", "Inspect accessible areas", "Record findings"], [], false)
        };

        services = records.ToDictionary(service => service.Id);
    }

    public IReadOnlyList<CatalogueService> All
    {
        get
        {
            lock (sync)
            {
                return services.Values.OrderBy(service => service.Name).ToArray();
            }
        }
    }

    public CatalogueService? Find(Guid id)
    {
        lock (sync)
        {
            return services.GetValueOrDefault(id);
        }
    }

    public CatalogueSaveResult Save(CatalogueService service)
    {
        lock (sync)
        {
            if (services.Values.Any(existing => existing.Id != service.Id &&
                existing.Code.Equals(service.Code, StringComparison.OrdinalIgnoreCase)))
            {
                return new(null, "A service with this code already exists.");
            }

            if (service.Dependencies.Contains(service.Id) || service.Dependencies.Any(id => !services.ContainsKey(id)))
            {
                return new(null, "Select valid dependencies. A service cannot depend on itself.");
            }

            foreach (var dependencyId in service.Dependencies)
            {
                if (DependsOn(dependencyId, service.Id, new HashSet<Guid>()))
                {
                    return new(null, "These dependencies create a circular work sequence. Review the selected services.");
                }
            }

            services[service.Id] = service;
            return new(service.Id, null);
        }
    }

    public bool SetActive(Guid id, bool isActive)
    {
        lock (sync)
        {
            if (!services.TryGetValue(id, out var service))
            {
                return false;
            }

            services[id] = service with { IsActive = isActive };
            return true;
        }
    }

    private bool DependsOn(Guid start, Guid target, HashSet<Guid> visited)
    {
        if (start == target)
        {
            return true;
        }

        return visited.Add(start) && services.TryGetValue(start, out var service)
            && service.Dependencies.Any(id => DependsOn(id, target, visited));
    }

    private static Guid ServiceId(int number) => Guid.Parse($"b17d0000-0000-4000-8000-{number:D12}");

    private static CatalogueService Seed(
        int number,
        string code,
        string name,
        string category,
        string unit,
        bool assessment,
        string description,
        string[] inclusions,
        string[] exclusions,
        string[] questions,
        string[] stages,
        Guid[] dependencies,
        bool active = true)
    {
        return new CatalogueService(
            ServiceId(number), code, name, category, unit, description,
            assessment, active, Array.AsReadOnly(inclusions), Array.AsReadOnly(exclusions),
            Array.AsReadOnly(questions), Array.AsReadOnly(stages), Array.AsReadOnly(dependencies));
    }
}

public sealed record CatalogueService(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string Unit,
    string Description,
    bool RequiresSiteAssessment,
    bool IsActive,
    IReadOnlyList<string> Inclusions,
    IReadOnlyList<string> Exclusions,
    IReadOnlyList<string> Questions,
    IReadOnlyList<string> Stages,
    IReadOnlyList<Guid> Dependencies);

public sealed record CatalogueSaveResult(Guid? Id, string? Error);
