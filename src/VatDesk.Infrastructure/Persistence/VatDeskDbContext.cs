using Microsoft.EntityFrameworkCore;
using VatDesk.Infrastructure.Persistence.Entities;

namespace VatDesk.Infrastructure.Persistence;

public class VatDeskDbContext(DbContextOptions<VatDeskDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<DeclarationEntity> Declarations => Set<DeclarationEntity>();
    public DbSet<DeclarationCategoryTotalEntity> DeclarationCategoryTotals => Set<DeclarationCategoryTotalEntity>();
    public DbSet<ValidationIssueEntity> ValidationIssues => Set<ValidationIssueEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.Email).HasColumnName("email").IsRequired();
            b.HasIndex(e => e.Email).IsUnique();
            b.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            b.Property(e => e.Role).HasColumnName("role").HasConversion<string>().IsRequired();
            b.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<DeclarationEntity>(b =>
        {
            b.ToTable("declarations");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id");
            b.Property(e => e.UserId).HasColumnName("user_id");
            b.Property(e => e.SourceFilename).HasColumnName("source_filename").IsRequired();
            b.Property(e => e.SourceFormat).HasColumnName("source_format").HasConversion<string>().IsRequired();
            b.Property(e => e.CountryCode).HasColumnName("country_code").IsRequired();
            b.Property(e => e.Status).HasColumnName("status").HasConversion<string>().IsRequired();
            b.Property(e => e.TotalOutputVat).HasColumnName("total_output_vat");
            b.Property(e => e.TotalInputVat).HasColumnName("total_input_vat");
            b.Property(e => e.NetVatPayable).HasColumnName("net_vat_payable");
            b.Property(e => e.ValidRows).HasColumnName("valid_rows");
            b.Property(e => e.WarningRows).HasColumnName("warning_rows");
            b.Property(e => e.ErrorRows).HasColumnName("error_rows");
            b.Property(e => e.CreatedAt).HasColumnName("created_at");

            b.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DeclarationCategoryTotalEntity>(b =>
        {
            b.ToTable("declaration_category_totals");
            b.HasKey(e => new { e.DeclarationId, e.VatCode, e.Direction });
            b.Property(e => e.DeclarationId).HasColumnName("declaration_id");
            b.Property(e => e.VatCode).HasColumnName("vat_code").IsRequired();
            b.Property(e => e.Direction).HasColumnName("direction").HasConversion<string>().IsRequired();
            b.Property(e => e.RowCount).HasColumnName("row_count");
            b.Property(e => e.TotalNet).HasColumnName("total_net");
            b.Property(e => e.TotalVat).HasColumnName("total_vat");
            b.Property(e => e.TotalGross).HasColumnName("total_gross");

            b.HasOne(e => e.Declaration)
                .WithMany(d => d.CategoryTotals)
                .HasForeignKey(e => e.DeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ValidationIssueEntity>(b =>
        {
            b.ToTable("validation_issues");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            b.Property(e => e.DeclarationId).HasColumnName("declaration_id");
            b.Property(e => e.RowNumber).HasColumnName("row_number");
            b.Property(e => e.RuleId).HasColumnName("rule_id").IsRequired();
            b.Property(e => e.Severity).HasColumnName("severity").HasConversion<string>().IsRequired();
            b.Property(e => e.Message).HasColumnName("message").IsRequired();

            b.HasOne(e => e.Declaration)
                .WithMany(d => d.ValidationIssues)
                .HasForeignKey(e => e.DeclarationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
