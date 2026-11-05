using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BuildWise.Data;

public partial class BuildWiseDbContext : DbContext
{
    public BuildWiseDbContext(DbContextOptions<BuildWiseDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<ClientRequest> ClientRequests { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<Quotation> Quotations { get; set; }

    public virtual DbSet<QuotationItem> QuotationItems { get; set; }

    public virtual DbSet<QuotationRevision> QuotationRevisions { get; set; }

    public virtual DbSet<ScopeClause> ScopeClauses { get; set; }

    public virtual DbSet<ScopeDependency> ScopeDependencies { get; set; }

    public virtual DbSet<ScopeItem> ScopeItems { get; set; }

    public virtual DbSet<ScopeStage> ScopeStages { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceClause> ServiceClauses { get; set; }

    public virtual DbSet<ServiceDependency> ServiceDependencies { get; set; }

    public virtual DbSet<ServiceStage> ServiceStages { get; set; }

    public virtual DbSet<ServiceUnit> ServiceUnits { get; set; }

    public virtual DbSet<Unit> Units { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserGroup> UserGroups { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("Client");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(50);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.NotesJson).HasColumnType("text");
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Creator).WithMany(p => p.Clients)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Client_User");
        });

        modelBuilder.Entity<ClientRequest>(entity =>
        {
            entity.ToTable("ClientRequest");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ConfirmationDate).HasColumnType("datetime");
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.PropertyType).HasMaxLength(50);
            entity.Property(e => e.ReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.SiteAddress).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(50);

            entity.HasOne(d => d.Client).WithMany(p => p.ClientRequests)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientRequest_Client");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.ClientRequestConfirmedByNavigations)
                .HasForeignKey(d => d.ConfirmedBy)
                .HasConstraintName("FK_ClientRequest_User");

            entity.HasOne(d => d.Creator).WithMany(p => p.ClientRequestCreators)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientRequest_User1");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.DefaultCurrency).HasMaxLength(3);
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.LogoStorageKey).HasMaxLength(256);
            entity.Property(e => e.Name).HasMaxLength(30);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.RegistrationNumber).HasMaxLength(50);
            entity.Property(e => e.TaxNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currency");

            entity.HasIndex(e => e.Code, "UQ_Currency_Code").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(3);
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Currency_CreationDate")
                .HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(80);
            entity.Property(e => e.Symbol).HasMaxLength(10);

            entity.HasOne(d => d.Creator).WithMany(p => p.Currencies)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Currency_User");
        });

        modelBuilder.Entity<Quotation>(entity =>
        {
            entity.ToTable("Quotation");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.QuotationNumber).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quotation_User");

            entity.HasOne(d => d.Request).WithMany(p => p.Quotations)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quotation_ClientRequest");
        });

        modelBuilder.Entity<QuotationItem>(entity =>
        {
            entity.ToTable("QuotationItem");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AssessmentNotesSnapshot).HasMaxLength(50);
            entity.Property(e => e.DependenciesSnapshotJson).HasColumnType("text");
            entity.Property(e => e.LineAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceCodeSnapshot).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.UnitCodeSnapshot).HasMaxLength(50);
            entity.Property(e => e.WorkStagesSnapshotJson).HasColumnType("text");

            entity.HasOne(d => d.QuotationRevision).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.QuotationRevisionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationItem_QuotationRevision");

            entity.HasOne(d => d.Request).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationItem_ClientRequest");

            entity.HasOne(d => d.ScopeItem).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.ScopeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationItem_ScopeItem");

            entity.HasOne(d => d.Service).WithMany(p => p.QuotationItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationItem_Service");
        });

        modelBuilder.Entity<QuotationRevision>(entity =>
        {
            entity.ToTable("QuotationRevision");

            entity.HasIndex(e => e.ApprovedByUserId, "IX_QuotationRevision_ApprovedByUserId");

            entity.HasIndex(e => e.CreatorId, "IX_QuotationRevision_CreatedByUserId");

            entity.HasIndex(e => new { e.QuotationId, e.RevisionNumber }, "UQ_QuotationRevision_Number").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApprovalDate).HasColumnType("datetime");
            entity.Property(e => e.CompanyAddressSnapshot).HasMaxLength(500);
            entity.Property(e => e.CompanyEmailSnapshot).HasMaxLength(256);
            entity.Property(e => e.CompanyLogoStorageKeySnapshot).HasMaxLength(500);
            entity.Property(e => e.CompanyNameSnapshot).HasMaxLength(150);
            entity.Property(e => e.CompanyPhoneSnapshot).HasMaxLength(40);
            entity.Property(e => e.CompanyRegistrationNumberSnapshot).HasMaxLength(80);
            entity.Property(e => e.CompanyTaxNumberSnapshot).HasMaxLength(80);
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_QuotationRevision_CreatedAtUtc")
                .HasColumnType("datetime");
            entity.Property(e => e.CurrencyCode).HasMaxLength(3);
            entity.Property(e => e.CustomerAddressSnapshot).HasMaxLength(500);
            entity.Property(e => e.CustomerEmailSnapshot).HasMaxLength(256);
            entity.Property(e => e.CustomerNameSnapshot).HasMaxLength(150);
            entity.Property(e => e.CustomerPhoneSnapshot).HasMaxLength(40);
            entity.Property(e => e.Exclusions).HasMaxLength(4000);
            entity.Property(e => e.IncludesPrices).HasDefaultValue(true, "DF_QuotationRevision_IncludesPrices");
            entity.Property(e => e.IssueDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdate).HasColumnType("datetime");
            entity.Property(e => e.SiteAddressSnapshot).HasMaxLength(500);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TaxTreatmentId).HasDefaultValue(0, "DF_QuotationRevision_TaxTreatment");
            entity.Property(e => e.Terms).HasMaxLength(4000);
            entity.Property(e => e.Title).HasMaxLength(150);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValidUntil).HasColumnType("datetime");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.QuotationRevisionApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .HasConstraintName("FK_QuotationRevision_ApprovedByUser");

            entity.HasOne(d => d.Creator).WithMany(p => p.QuotationRevisionCreators)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationRevision_User");

            entity.HasOne(d => d.Quotation).WithMany(p => p.QuotationRevisions)
                .HasForeignKey(d => d.QuotationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QuotationRevision_Quotation");
        });

        modelBuilder.Entity<ScopeClause>(entity =>
        {
            entity.ToTable("ScopeClause");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.ScopeClauses)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeClause_User");

            entity.HasOne(d => d.ScopeItem).WithMany(p => p.ScopeClauses)
                .HasForeignKey(d => d.ScopeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeClause_ScopeItem");
        });

        modelBuilder.Entity<ScopeDependency>(entity =>
        {
            entity.ToTable("ScopeDependency");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.NotesJson).HasColumnType("text");

            entity.HasOne(d => d.Creator).WithMany(p => p.ScopeDependencies)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeDependency_User");

            entity.HasOne(d => d.DependsOnScopeItem).WithMany(p => p.ScopeDependencyDependsOnScopeItems)
                .HasForeignKey(d => d.DependsOnScopeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeDependency_ScopeItem1");

            entity.HasOne(d => d.Request).WithMany(p => p.ScopeDependencies)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeDependency_ClientRequest");

            entity.HasOne(d => d.ScopeItem).WithMany(p => p.ScopeDependencyScopeItems)
                .HasForeignKey(d => d.ScopeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeDependency_ScopeItem");
        });

        modelBuilder.Entity<ScopeItem>(entity =>
        {
            entity.ToTable("ScopeItem");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AssessmentNotesJson).HasColumnType("text");
            entity.Property(e => e.ReviewedOn).HasColumnType("datetime");
            entity.Property(e => e.ServiceCodeSnapshot).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.UnitCodeSnapshot).HasMaxLength(50);

            entity.HasOne(d => d.Request).WithMany(p => p.ScopeItems)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeItem_ClientRequest");

            entity.HasOne(d => d.ReviewedByUser).WithMany(p => p.ScopeItems)
                .HasForeignKey(d => d.ReviewedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeItem_User");

            entity.HasOne(d => d.Service).WithMany(p => p.ScopeItems)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeItem_Service");
        });

        modelBuilder.Entity<ScopeStage>(entity =>
        {
            entity.ToTable("ScopeStage");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.ScopeStages)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeStage_User");

            entity.HasOne(d => d.ScopeItem).WithMany(p => p.ScopeStages)
                .HasForeignKey(d => d.ScopeItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ScopeStage_ScopeItem");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("Service");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.Category).WithMany(p => p.Services)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Service_ServiceCategory");

            entity.HasOne(d => d.Creator).WithMany(p => p.Services)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Service_User");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.ToTable("ServiceCategory");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ServiceClause>(entity =>
        {
            entity.ToTable("ServiceClause");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceClauses)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceClause_Service");
        });

        modelBuilder.Entity<ServiceDependency>(entity =>
        {
            entity.ToTable("ServiceDependency");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");

            entity.HasOne(d => d.Creator).WithMany(p => p.ServiceDependencies)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_ServiceDependency_User");

            entity.HasOne(d => d.DependsOnService).WithMany(p => p.ServiceDependencyDependsOnServices)
                .HasForeignKey(d => d.DependsOnServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDependency_Service1");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceDependencyServices)
                .HasForeignKey(d => d.ServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ServiceDependency_Service");
        });

        modelBuilder.Entity<ServiceStage>(entity =>
        {
            entity.ToTable("ServiceStage");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.ServiceStages)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_ServiceStage_User");

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceStages)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_ServiceStage_Service");
        });

        modelBuilder.Entity<ServiceUnit>(entity =>
        {
            entity.ToTable("ServiceUnit");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Service).WithMany(p => p.ServiceUnits)
                .HasForeignKey(d => d.ServiceId)
                .HasConstraintName("FK_ServiceUnit_Service");

            entity.HasOne(d => d.Unit).WithMany(p => p.ServiceUnits)
                .HasForeignKey(d => d.UnitId)
                .HasConstraintName("FK_ServiceUnit_Unit");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.ToTable("Unit");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.Units)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Unit_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ActivationDate).HasColumnType("datetime");
            entity.Property(e => e.AuthRecoveryCodes).HasMaxLength(50);
            entity.Property(e => e.AuthenticationKey).HasMaxLength(50);
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.LastLoginDate).HasColumnType("datetime");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.SecurityStamp).HasMaxLength(50);
            entity.Property(e => e.Surname).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.InverseCreator)
                .HasForeignKey(d => d.CreatorId)
                .HasConstraintName("FK_User_User");

            entity.HasOne(d => d.Group).WithMany(p => p.Users)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK_User_UserGroup");
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("UserGroup");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasColumnType("datetime");
            entity.Property(e => e.Name).HasMaxLength(50);

            entity.HasOne(d => d.Creator).WithMany(p => p.UserGroups)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserGroup_User");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("UserSession");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.LockoutDate).HasColumnType("datetime");
            entity.Property(e => e.LoginDate).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithOne(p => p.UserSession)
                .HasForeignKey<UserSession>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserSession_User");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
