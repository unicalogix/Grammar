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
            await db.SaveChangesAsync();

        Grade grade;
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
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample Grade 5");
        }
        else
        {
            grade = await db.Grades.OrderBy(g => g.SortOrder).FirstAsync();
        }

        var tense = await EnsureRuleAsync(db, logger, admin.Id, "TENSE", "Tense", "زمانہ", sortOrder: 1);
        var speech = await EnsureRuleAsync(db, logger, admin.Id,
            "TYPES_OF_SPEECH", "Types of Speech", "کلام کی اقسام", sortOrder: 2,
            legacyCodes: ["DIRECT_INDIRECT"],
            legacyNames: ["Direct and Indirect Speech"]);

        var pastSimple = await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, tense.Id,
            "PAST_SIMPLE", "Past Simple", "ماضی مطلق",
            "Used for completed actions in the past.", "ماضی میں مکمل ہونے والے کاموں کے لیے۔", 10);
        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, tense.Id,
            "PRESENT_SIMPLE", "Simple Present", "حال مطلق",
            "Used for habits and general truths.", "عادتوں اور عام سچائیوں کے لیے۔", 20);
        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, tense.Id,
            "PRESENT_CONTINUOUS", "Present Continuous", "حال جاری",
            "Used for actions happening now.", "ابھی ہو رہے کاموں کے لیے۔", 30);
        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, tense.Id,
            "PRESENT_PERFECT", "Present Perfect", "حال کامل",
            "Used for past actions with present relevance.", "ماضی کے کام جو حال سے تعلق رکھتے ہیں۔", 40);
        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, tense.Id,
            "FUTURE_SIMPLE", "Future Simple", "مستقبل مطلق",
            "Used for actions that will happen later.", "آنے والے کاموں کے لیے۔", 50);

        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, speech.Id,
            "DIRECT_SPEECH", "Direct Speech", "براہ راست کلام",
            "Exact words of the speaker in quotation marks.", "اسپیکر کے اصل الفاظ۔", 10);
        await EnsureRuleDetailAsync(db, logger, admin.Id, grade.Id, speech.Id,
            "INDIRECT_SPEECH", "Indirect Speech", "بالواسطہ کلام",
            "Reported speech without quotation marks.", "رپورٹ شدہ کلام۔", 20);

        // Rename legacy Present Simple label if still using old wording
        var presentSimple = await db.RuleDetails.FirstOrDefaultAsync(r => r.Code == "PRESENT_SIMPLE");
        if (presentSimple is not null && presentSimple.NameEn == "Present Simple")
        {
            presentSimple.NameEn = "Simple Present";
            presentSimple.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

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
                db.SentenceRules.Add(new SentenceRule { SentenceId = sentence.Id, RuleDetailId = pastSimple.Id });
            }
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} approved sample sentences for practice", samples.Length);
        }

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
                ShowEnglish = true,
                ShowTranslation = false,
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
                ShowEnglish = true,
                ShowTranslation = false,
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
                ShowEnglish = true,
                ShowTranslation = true,
                ItemCount = 6,
                IsActive = true,
                CreatedByUserId = admin.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample saved tests");
        }
        else
        {
            // Backfill prompt flags on older saved tests
            var needsFlags = await db.SavedTests
                .Where(t => !t.ShowEnglish && !t.ShowTranslation)
                .ToListAsync();
            foreach (var t in needsFlags)
            {
                t.ShowEnglish = t.SourceLanguage != "ur";
                t.ShowTranslation = t.SourceLanguage == "ur";
                t.UpdatedAtUtc = DateTime.UtcNow;
            }
            if (needsFlags.Count > 0)
                await db.SaveChangesAsync();
        }
    }

    private static async Task<Rule> EnsureRuleAsync(
        AppDbContext db, ILogger logger, string adminId,
        string code, string nameEn, string nameUr, int sortOrder,
        string[]? legacyCodes = null,
        string[]? legacyNames = null)
    {
        var existing = await db.Rules.FirstOrDefaultAsync(r => r.Code == code);
        if (existing is null && legacyCodes is not null)
        {
            foreach (var legacy in legacyCodes)
            {
                existing = await db.Rules.FirstOrDefaultAsync(r => r.Code == legacy);
                if (existing is not null) break;
            }
        }

        if (existing is null)
        {
            existing = await db.Rules.FirstOrDefaultAsync(r => r.NameEn == nameEn);
            if (existing is null && legacyNames is not null)
            {
                foreach (var legacy in legacyNames)
                {
                    existing = await db.Rules.FirstOrDefaultAsync(r => r.NameEn == legacy);
                    if (existing is not null) break;
                }
            }
        }

        if (existing is not null)
        {
            var changed = false;
            if (existing.Code != code)
            {
                existing.Code = code;
                changed = true;
            }
            if (existing.NameEn != nameEn)
            {
                existing.NameEn = nameEn;
                changed = true;
            }
            if (existing.NameUr != nameUr)
            {
                existing.NameUr = nameUr;
                changed = true;
            }
            if (changed)
            {
                existing.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            return existing;
        }

        existing = new Rule
        {
            Code = code,
            NameEn = nameEn,
            NameUr = nameUr,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = adminId
        };
        db.Rules.Add(existing);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded rule {Code}", code);
        return existing;
    }

    private static async Task<RuleDetail> EnsureRuleDetailAsync(
        AppDbContext db,
        ILogger logger,
        string adminId,
        int gradeId,
        int ruleId,
        string code,
        string nameEn,
        string nameUr,
        string explanationEn,
        string explanationUr,
        int sortOrder)
    {
        var existing = await db.RuleDetails.FirstOrDefaultAsync(r => r.Code == code);
        if (existing is null)
        {
            existing = new RuleDetail
            {
                RuleId = ruleId,
                NameEn = nameEn,
                NameUr = nameUr,
                ExplanationEn = explanationEn,
                ExplanationUr = explanationUr,
                Code = code,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = adminId
            };
            db.RuleDetails.Add(existing);
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded rule detail {Code}", code);
        }
        else if (existing.RuleId != ruleId)
        {
            existing.RuleId = ruleId;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        if (!await db.GradeRules.AnyAsync(gr => gr.GradeId == gradeId && gr.RuleDetailId == existing.Id))
        {
            db.GradeRules.Add(new GradeRule { GradeId = gradeId, RuleDetailId = existing.Id });
            await db.SaveChangesAsync();
        }

        return existing;
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
