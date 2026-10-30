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
    public DbSet<RuleDetail> RuleDetails => Set<RuleDetail>();
    public DbSet<GradeRule> GradeRules => Set<GradeRule>();
    public DbSet<Sentence> Sentences => Set<Sentence>();
    public DbSet<SentenceRule> SentenceRules => Set<SentenceRule>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<SavedTest> SavedTests => Set<SavedTest>();

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
            e.ToTable("Rules");
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameUr).HasMaxLength(200);
            e.Property(x => x.Code).HasMaxLength(64);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasIndex(x => x.SortOrder);
            e.HasIndex(x => x.IsActive);
        });

        builder.Entity<RuleDetail>(e =>
        {
            e.ToTable("RuleDetails");
            e.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameUr).HasMaxLength(200);
            e.Property(x => x.ExplanationEn).HasMaxLength(1000);
            e.Property(x => x.ExplanationUr).HasMaxLength(1000);
            e.Property(x => x.Code).HasMaxLength(64);
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => new { x.RuleId, x.SortOrder });
            e.HasOne(x => x.Rule).WithMany(r => r.Details).HasForeignKey(x => x.RuleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GradeRule>(e =>
        {
            e.HasKey(x => new { x.GradeId, x.RuleDetailId });
            e.HasOne(x => x.Grade).WithMany(g => g.GradeRules).HasForeignKey(x => x.GradeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RuleDetail).WithMany(r => r.GradeRules).HasForeignKey(x => x.RuleDetailId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RuleDetailId);
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
            e.HasKey(x => new { x.SentenceId, x.RuleDetailId });
            e.HasOne(x => x.Sentence).WithMany(s => s.SentenceRules).HasForeignKey(x => x.SentenceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RuleDetail).WithMany().HasForeignKey(x => x.RuleDetailId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.RuleDetailId);
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

        builder.Entity<PracticeSession>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.SourceLanguage).HasMaxLength(2).IsRequired().IsFixedLength();
            e.Property(x => x.TargetLanguage).HasMaxLength(2).IsRequired().IsFixedLength();
            e.HasIndex(x => new { x.UserId, x.StartedAtUtc });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SavedTest).WithMany().HasForeignKey(x => x.SavedTestId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SavedTest>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.ExerciseTypesJson).HasMaxLength(200).IsRequired();
            e.Property(x => x.GradeIdsJson).HasMaxLength(500).IsRequired();
            e.Property(x => x.RuleIdsJson).HasMaxLength(500);
            e.Property(x => x.SourceLanguage).HasMaxLength(2).IsRequired().IsFixedLength();
            e.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.Layout).HasConversion<byte>();
            e.Property(x => x.DropdownMode).HasConversion<byte>();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => new { x.CreatedByUserId, x.CreatedAtUtc });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Attempt>(e =>
        {
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.PromptLanguage).HasMaxLength(2).IsRequired().IsFixedLength();
            e.Property(x => x.ExpectedAnswer).HasMaxLength(500).IsRequired();
            e.Property(x => x.StudentAnswer).HasMaxLength(500).IsRequired();
            e.Property(x => x.NormalizedExpected).HasMaxLength(500).IsRequired();
            e.Property(x => x.NormalizedStudent).HasMaxLength(500).IsRequired();
            e.Property(x => x.ExerciseType).HasConversion<byte>();
            e.HasOne(x => x.PracticeSession).WithMany(s => s.Attempts)
                .HasForeignKey(x => x.PracticeSessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Sentence).WithMany().HasForeignKey(x => x.SentenceId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PracticeSessionId, x.SequenceNo });
            e.HasIndex(x => new { x.UserId, x.AnsweredAtUtc });
            e.HasIndex(x => x.SentenceId);
        });
    }
}
