using EnUrGrammar.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@enur.local";
    public const string DefaultAdminPassword = "ChangeMe!Admin123";
    public const string DefaultStudentEmail = "student@enur.local";
    public const string DefaultStudentPassword = "ChangeMe!Student123";

    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        var admin = await EnsureUserAsync(
            userManager, logger, DefaultAdminEmail, DefaultAdminPassword, "Sample Admin", AppRoles.Admin);
        await EnsureUserAsync(
            userManager, logger, DefaultStudentEmail, DefaultStudentPassword, "Sample Student", AppRoles.Student);

        // Backfill Code on existing seed grade if missing (Phase 0 → Phase 1)
        var ungoded = await db.Grades.Where(g => g.NameEn == "Grade 5" && g.Code == null).ToListAsync();
        foreach (var g in ungoded)
        {
            g.Code = "G5";
            g.UpdatedAtUtc = DateTime.UtcNow;
        }
        if (ungoded.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        Grade grade;
        Rule rule;
        if (!await db.Grades.AnyAsync())
        {
            grade = new Grade
            {
                NameEn = "Grade 5",
                NameUr = "جماعت ۵",
                Code = "G5",
                SortOrder = 5,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = admin.Id
            };
            db.Grades.Add(grade);

            rule = new Rule
            {
                NameEn = "Past Simple",
                NameUr = "ماضی مطلق",
                ExplanationEn = "Used for completed actions in the past.",
                ExplanationUr = "ماضی میں مکمل ہونے والے کاموں کے لیے۔",
                Code = "PAST_SIMPLE",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = admin.Id
            };
            db.Rules.Add(rule);
            await db.SaveChangesAsync();

            db.GradeRules.Add(new GradeRule { GradeId = grade.Id, RuleId = rule.Id });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample Grade 5 and PAST_SIMPLE rule");
        }
        else
        {
            grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();
            rule = await db.Rules.FirstOrDefaultAsync(r => r.Code == "PAST_SIMPLE")
                   ?? await db.Rules.FirstAsync();
        }

        // Phase 2b: extra rules so “Which rules apply?” has distractors
        await EnsureRuleLinkedAsync(db, logger, admin.Id, grade.Id,
            "PRESENT_SIMPLE", "Present Simple", "حال مطلق",
            "Used for habits and general truths.", "عادتوں اور عام سچائیوں کے لیے۔");
        await EnsureRuleLinkedAsync(db, logger, admin.Id, grade.Id,
            "FUTURE_SIMPLE", "Future Simple", "مستقبل مطلق",
            "Used for actions that will happen later.", "آنے والے کاموں کے لیے۔");
        await EnsureRuleLinkedAsync(db, logger, admin.Id, grade.Id,
            "PRESENT_CONTINUOUS", "Present Continuous", "حال جاری",
            "Used for actions happening now.", "ابھی ہو رہے کاموں کے لیے۔");

        // Phase 2a: approved sample sentences so practice is non-empty out of the box
        if (!await db.Sentences.AnyAsync(s => !s.IsDeleted && s.Status == SentenceStatus.Approved))
        {
            var samples = new (string En, string Ur)[]
            {
                ("She walked to school.", "وہ اسکول گئی۔"),
                ("They played football yesterday.", "انہوں نے کل فٹ بال کھیلا۔"),
                ("I finished my homework.", "میں نے اپنا ہوم ورک مکمل کیا۔"),
                ("He opened the door.", "اس نے دروازہ کھولا۔"),
                ("We visited the museum.", "ہم میوزیم گئے۔"),
                ("The cat jumped on the table.", "بلی میز پر کودی۔")
            };

            foreach (var (en, ur) in samples)
            {
                var sentence = new Sentence
                {
                    TextEn = en,
                    TextUr = ur,
                    GradeId = grade.Id,
                    Difficulty = Difficulty.Easy,
                    Status = SentenceStatus.Approved,
                    CreatedAtUtc = DateTime.UtcNow,
                    ApprovedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = admin.Id,
                    ApprovedByUserId = admin.Id
                };
                db.Sentences.Add(sentence);
                await db.SaveChangesAsync();
                db.SentenceRules.Add(new SentenceRule { SentenceId = sentence.Id, RuleId = rule.Id });
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} approved sample sentences for practice", samples.Length);
        }

        // Saved Tests samples (grid + sequential) for admin/student try-out
        if (!await db.SavedTests.AnyAsync())
        {
            db.SavedTests.Add(new SavedTest
            {
                Name = "Grade 5 · Rules grid",
                Description = "Recognize which tense/rule applies — all questions on one page.",
                Layout = TestLayout.Grid,
                DropdownMode = DropdownMode.Rules,
                ExerciseTypesJson = "[]",
                GradeIdsJson = $"[{grade.Id}]",
                RuleIdsJson = null,
                Difficulty = null,
                SourceLanguage = "en",
                ItemCount = 6,
                IsActive = true,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            db.SavedTests.Add(new SavedTest
            {
                Name = "Grade 5 · Translation grid",
                Description = "Pick the correct translation from a dropdown for each sentence.",
                Layout = TestLayout.Grid,
                DropdownMode = DropdownMode.Translation,
                ExerciseTypesJson = "[]",
                GradeIdsJson = $"[{grade.Id}]",
                RuleIdsJson = null,
                Difficulty = null,
                SourceLanguage = "en",
                ItemCount = 6,
                IsActive = true,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            db.SavedTests.Add(new SavedTest
            {
                Name = "Grade 5 · Blanks & arrange",
                Description = "Sequential session with fill-blank and arrange-with-distractors only.",
                Layout = TestLayout.Sequential,
                DropdownMode = DropdownMode.None,
                ExerciseTypesJson = "[4,5]",
                GradeIdsJson = $"[{grade.Id}]",
                RuleIdsJson = null,
                Difficulty = null,
                SourceLanguage = "en",
                ItemCount = 6,
                IsActive = true,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample saved tests");
        }
    }

    private static async Task EnsureRuleLinkedAsync(
        AppDbContext db,
        ILogger logger,
        string adminId,
        int gradeId,
        string code,
        string nameEn,
        string nameUr,
        string explanationEn,
        string explanationUr)
    {
        var existing = await db.Rules.FirstOrDefaultAsync(r => r.Code == code);
        if (existing is null)
        {
            existing = new Rule
            {
                NameEn = nameEn,
                NameUr = nameUr,
                ExplanationEn = explanationEn,
                ExplanationUr = explanationUr,
                Code = code,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = adminId
            };
            db.Rules.Add(existing);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded rule {Code}", code);
        }

        if (!await db.GradeRules.AnyAsync(gr => gr.GradeId == gradeId && gr.RuleId == existing.Id))
        {
            db.GradeRules.Add(new GradeRule { GradeId = gradeId, RuleId = existing.Id });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string displayName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user {email}: {string.Join("; ", create.Errors.Select(e => e.Description))}");
            }

            logger.LogInformation("Seeded user {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign {role} to {email}: {string.Join("; ", addRole.Errors.Select(e => e.Description))}");
            }
        }

        return user;
    }
}
