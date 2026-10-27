using EnUrGrammar.Api.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<GradeRule> GradeRules => Set<GradeRule>();
    public DbSet<Sentence> Sentences => Set<Sentence>();
    public DbSet<SentenceRule> SentenceRules => Set<SentenceRule>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Grade>(e =>
        {
            e.Property(x => x.NameEn).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameUr).HasMaxLength(100);
            e.Property(x => x.Code).HasMaxLength(64);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasIndex(x => x.SortOrder);
            e.HasIndex(x => x.IsActive);
        });

        builder.Entity<Rule>(e =>
        {
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameUr).HasMaxLength(200);
            e.Property(x => x.ExplanationEn).HasMaxLength(1000);
            e.Property(x => x.ExplanationUr).HasMaxLength(1000);
            e.Property(x => x.Code).HasMaxLength(64);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasIndex(x => x.IsActive);
        });

        builder.Entity<GradeRule>(e =>
        {
            e.HasKey(x => new { x.GradeId, x.RuleId });
            e.HasOne(x => x.Grade).WithMany(g => g.GradeRules).HasForeignKey(x => x.GradeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Rule).WithMany(r => r.GradeRules).HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RuleId);
        });

        builder.Entity<Sentence>(e =>
        {
            e.Property(x => x.TextEn).HasMaxLength(500).IsRequired();
            e.Property(x => x.TextUr).HasMaxLength(500).IsRequired();
            e.Property(x => x.MtSuggestedUr).HasMaxLength(500);
            e.Property(x => x.MtSuggestedEn).HasMaxLength(500);
            e.Property(x => x.Difficulty).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasOne(x => x.Grade).WithMany(g => g.Sentences).HasForeignKey(x => x.GradeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.GradeId, x.Status, x.Difficulty });
            e.HasIndex(x => x.Status);
        });

        builder.Entity<SentenceRule>(e =>
        {
            e.HasKey(x => new { x.SentenceId, x.RuleId });
            e.HasOne(x => x.Sentence).WithMany(s => s.SentenceRules).HasForeignKey(x => x.SentenceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Rule).WithMany().HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RuleId);
        });

        builder.Entity<ImportBatch>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.Property(x => x.UploadedByUserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.EntityKind).HasConversion<byte>();
            e.Property(x => x.Status).HasConversion<byte>();
            e.HasIndex(x => new { x.UploadedByUserId, x.CreatedAtUtc });
        });
    }
}
