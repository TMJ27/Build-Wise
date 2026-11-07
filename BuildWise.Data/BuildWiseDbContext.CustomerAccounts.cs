using Microsoft.EntityFrameworkCore;

namespace BuildWise.Data;

public partial class BuildWiseDbContext
{
    public DbSet<CustomerAccount> CustomerAccounts { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerAccount>(entity =>
        {
            entity.ToTable("CustomerAccount");
            entity.Property(account => account.Id).ValueGeneratedNever();
            entity.Property(account => account.Email).HasMaxLength(50).IsRequired();
            entity.Property(account => account.NormalizedEmail).HasMaxLength(50).IsRequired();
            entity.Property(account => account.PasswordHash).HasMaxLength(1000).IsRequired();
            entity.Property(account => account.SecurityStamp).HasMaxLength(64).IsRequired();
            entity.Property(account => account.CreationDate).HasColumnType("datetime");
            entity.HasIndex(account => account.NormalizedEmail).IsUnique();
            entity.HasIndex(account => account.ClientId).IsUnique();
            entity.HasIndex(account => account.UserId).IsUnique();
            entity.HasOne(account => account.Client).WithMany().HasForeignKey(account => account.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(account => account.User).WithMany().HasForeignKey(account => account.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
