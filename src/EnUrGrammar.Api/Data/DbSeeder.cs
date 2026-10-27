using EnUrGrammar.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnUrGrammar.Api.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@enur.local";
    public const string DefaultAdminPassword = "ChangeMe!Admin123";

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

        var admin = await userManager.FindByEmailAsync(DefaultAdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Sample Admin",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var create = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin user: {string.Join("; ", create.Errors.Select(e => e.Description))}");
            }

            var addRole = await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            if (!addRole.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to assign Admin role: {string.Join("; ", addRole.Errors.Select(e => e.Description))}");
            }

            logger.LogInformation("Seeded admin user {Email}", DefaultAdminEmail);
        }

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

        if (!await db.Grades.AnyAsync())
        {
            var grade = new Grade
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

            var rule = new Rule
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
    }
}
