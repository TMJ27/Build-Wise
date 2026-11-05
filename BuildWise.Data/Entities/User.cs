using System;
using System.Collections.Generic;

namespace BuildWise.Data;

public partial class User
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public bool IsActive { get; set; }

    public bool IsEmailConfirmed { get; set; }

    public Guid? GroupId { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? SecurityStamp { get; set; }

    public string? AuthenticationKey { get; set; }

    public string? AuthRecoveryCodes { get; set; }

    public bool? TwoFactorAuthEnabled { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public int? AccessFailedCount { get; set; }

    public DateTime? ActivationDate { get; set; }

    public DateTime CreationDate { get; set; }

    public DateTime? LastUpdated { get; set; }

    public Guid? CreatorId { get; set; }

    public virtual ICollection<ClientRequest> ClientRequestConfirmedByNavigations { get; set; } = new List<ClientRequest>();

    public virtual ICollection<ClientRequest> ClientRequestCreators { get; set; } = new List<ClientRequest>();

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    public virtual User? Creator { get; set; }

    public virtual ICollection<Currency> Currencies { get; set; } = new List<Currency>();

    public virtual UserGroup? Group { get; set; }

    public virtual ICollection<User> InverseCreator { get; set; } = new List<User>();

    public virtual ICollection<QuotationRevision> QuotationRevisionApprovedByUsers { get; set; } = new List<QuotationRevision>();

    public virtual ICollection<QuotationRevision> QuotationRevisionCreators { get; set; } = new List<QuotationRevision>();

    public virtual ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    public virtual ICollection<ScopeClause> ScopeClauses { get; set; } = new List<ScopeClause>();

    public virtual ICollection<ScopeDependency> ScopeDependencies { get; set; } = new List<ScopeDependency>();

    public virtual ICollection<ScopeItem> ScopeItems { get; set; } = new List<ScopeItem>();

    public virtual ICollection<ScopeStage> ScopeStages { get; set; } = new List<ScopeStage>();

    public virtual ICollection<ServiceDependency> ServiceDependencies { get; set; } = new List<ServiceDependency>();

    public virtual ICollection<ServiceStage> ServiceStages { get; set; } = new List<ServiceStage>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();

    public virtual ICollection<Unit> Units { get; set; } = new List<Unit>();

    public virtual ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();

    public virtual UserSession? UserSession { get; set; }
}
