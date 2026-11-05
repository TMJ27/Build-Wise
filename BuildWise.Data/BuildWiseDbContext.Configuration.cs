using Microsoft.EntityFrameworkCore;

namespace BuildWise.Data;

public partial class BuildWiseDbContext
{
    /*public virtual DbSet<Currency> Currencies { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>()
            .Property(company => company.LastUpdated)
            .IsConcurrencyToken();

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.ToTable("Currency");
            entity.HasKey(currency => currency.Id);
            entity.Property(currency => currency.Id).ValueGeneratedNever();
            entity.Property(currency => currency.Name).HasMaxLength(80);
            entity.Property(currency => currency.Code).HasMaxLength(3);
            entity.Property(currency => currency.Symbol).HasMaxLength(10);
            entity.Property(currency => currency.CreationDate)
                .HasColumnType("datetime2(3)")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasAlternateKey(currency => currency.Code).HasName("UQ_Currency_Code");
            entity.HasOne(currency => currency.Creator)
                .WithMany()
                .HasForeignKey(currency => currency.CreatorId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_Currency_User");
        });
    }*/
}
