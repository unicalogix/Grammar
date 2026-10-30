using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnUrGrammar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RuleCategoriesAndPromptFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prompt language flags on saved tests (English checked by default)
            migrationBuilder.AddColumn<bool>(
                name: "ShowEnglish",
                table: "SavedTests",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowTranslation",
                table: "SavedTests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill from SourceLanguage for any rows that somehow get both false
            migrationBuilder.Sql("""
                UPDATE SavedTests
                SET ShowEnglish = CASE WHEN SourceLanguage = N'ur' THEN 0 ELSE 1 END,
                    ShowTranslation = CASE WHEN SourceLanguage = N'ur' THEN 1 ELSE 0 END
                WHERE ShowEnglish = 0 AND ShowTranslation = 0;
                """);

            migrationBuilder.CreateTable(
                name: "RuleCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameUr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameUr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExplanationUr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleItems_RuleCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RuleCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleCategories_Code",
                table: "RuleCategories",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCategories_IsActive",
                table: "RuleCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RuleCategories_SortOrder",
                table: "RuleCategories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_RuleItems_CategoryId_SortOrder",
                table: "RuleItems",
                columns: new[] { "CategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleItems_Code",
                table: "RuleItems",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RuleItems_IsActive",
                table: "RuleItems",
                column: "IsActive");

            // Preserve existing Rules as RuleItems under a Tense category (same Ids).
            migrationBuilder.Sql("""
                DECLARE @TenseId INT;

                IF NOT EXISTS (SELECT 1 FROM RuleCategories WHERE Code = N'TENSE')
                BEGIN
                    INSERT INTO RuleCategories (NameEn, NameUr, Code, SortOrder, IsActive, CreatedAtUtc, CreatedByUserId)
                    VALUES (N'Tense', N'زمانہ', N'TENSE', 1, 1, SYSUTCDATETIME(), NULL);
                END

                SELECT @TenseId = Id FROM RuleCategories WHERE Code = N'TENSE';

                SET IDENTITY_INSERT RuleItems ON;
                INSERT INTO RuleItems (
                    Id, CategoryId, NameEn, NameUr, ExplanationEn, ExplanationUr,
                    Code, SortOrder, IsActive, CreatedAtUtc, UpdatedAtUtc, CreatedByUserId)
                SELECT
                    r.Id,
                    @TenseId,
                    r.NameEn,
                    r.NameUr,
                    r.ExplanationEn,
                    r.ExplanationUr,
                    r.Code,
                    r.Id * 10,
                    r.IsActive,
                    r.CreatedAtUtc,
                    r.UpdatedAtUtc,
                    r.CreatedByUserId
                FROM Rules r
                WHERE NOT EXISTS (SELECT 1 FROM RuleItems ri WHERE ri.Id = r.Id);
                SET IDENTITY_INSERT RuleItems OFF;

                -- Ensure seed Direct/Indirect category exists (items added by DbSeeder).
                IF NOT EXISTS (SELECT 1 FROM RuleCategories WHERE Code = N'DIRECT_INDIRECT')
                BEGIN
                    INSERT INTO RuleCategories (NameEn, NameUr, Code, SortOrder, IsActive, CreatedAtUtc, CreatedByUserId)
                    VALUES (N'Direct and Indirect Speech', N'براہ راست اور بالواسطہ کلام', N'DIRECT_INDIRECT', 2, 1, SYSUTCDATETIME(), NULL);
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_GradeRules_Rules_RuleId",
                table: "GradeRules");

            migrationBuilder.DropForeignKey(
                name: "FK_SentenceRules_Rules_RuleId",
                table: "SentenceRules");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.RenameColumn(
                name: "RuleId",
                table: "SentenceRules",
                newName: "RuleItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SentenceRules_RuleId",
                table: "SentenceRules",
                newName: "IX_SentenceRules_RuleItemId");

            migrationBuilder.RenameColumn(
                name: "RuleId",
                table: "GradeRules",
                newName: "RuleItemId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRules_RuleId",
                table: "GradeRules",
                newName: "IX_GradeRules_RuleItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRules_RuleItems_RuleItemId",
                table: "GradeRules",
                column: "RuleItemId",
                principalTable: "RuleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SentenceRules_RuleItems_RuleItemId",
                table: "SentenceRules",
                column: "RuleItemId",
                principalTable: "RuleItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeRules_RuleItems_RuleItemId",
                table: "GradeRules");

            migrationBuilder.DropForeignKey(
                name: "FK_SentenceRules_RuleItems_RuleItemId",
                table: "SentenceRules");

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExplanationUr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameUr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                });

            migrationBuilder.Sql("""
                SET IDENTITY_INSERT Rules ON;
                INSERT INTO Rules (
                    Id, NameEn, NameUr, ExplanationEn, ExplanationUr, Code, IsActive,
                    CreatedAtUtc, UpdatedAtUtc, CreatedByUserId)
                SELECT
                    Id, NameEn, NameUr, ExplanationEn, ExplanationUr, Code, IsActive,
                    CreatedAtUtc, UpdatedAtUtc, CreatedByUserId
                FROM RuleItems;
                SET IDENTITY_INSERT Rules OFF;
                """);

            migrationBuilder.DropTable(
                name: "RuleItems");

            migrationBuilder.DropTable(
                name: "RuleCategories");

            migrationBuilder.DropColumn(
                name: "ShowEnglish",
                table: "SavedTests");

            migrationBuilder.DropColumn(
                name: "ShowTranslation",
                table: "SavedTests");

            migrationBuilder.RenameColumn(
                name: "RuleItemId",
                table: "SentenceRules",
                newName: "RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_SentenceRules_RuleItemId",
                table: "SentenceRules",
                newName: "IX_SentenceRules_RuleId");

            migrationBuilder.RenameColumn(
                name: "RuleItemId",
                table: "GradeRules",
                newName: "RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRules_RuleItemId",
                table: "GradeRules",
                newName: "IX_GradeRules_RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_Code",
                table: "Rules",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_IsActive",
                table: "Rules",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRules_Rules_RuleId",
                table: "GradeRules",
                column: "RuleId",
                principalTable: "Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SentenceRules_Rules_RuleId",
                table: "SentenceRules",
                column: "RuleId",
                principalTable: "Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
