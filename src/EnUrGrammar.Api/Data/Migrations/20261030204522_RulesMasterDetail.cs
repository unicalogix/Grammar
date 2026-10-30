using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnUrGrammar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RulesMasterDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop join FKs before renaming leaf table / columns.
            migrationBuilder.DropForeignKey(
                name: "FK_GradeRules_RuleItems_RuleItemId",
                table: "GradeRules");

            migrationBuilder.DropForeignKey(
                name: "FK_SentenceRules_RuleItems_RuleItemId",
                table: "SentenceRules");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleItems_RuleCategories_CategoryId",
                table: "RuleItems");

            // RuleCategories → Rules (master)
            migrationBuilder.RenameTable(
                name: "RuleCategories",
                newName: "Rules");

            migrationBuilder.RenameIndex(
                name: "IX_RuleCategories_Code",
                table: "Rules",
                newName: "IX_Rules_Code");

            migrationBuilder.RenameIndex(
                name: "IX_RuleCategories_IsActive",
                table: "Rules",
                newName: "IX_Rules_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_RuleCategories_SortOrder",
                table: "Rules",
                newName: "IX_Rules_SortOrder");

            // RuleItems → RuleDetails (detail)
            migrationBuilder.RenameTable(
                name: "RuleItems",
                newName: "RuleDetails");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "RuleDetails",
                newName: "RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_RuleItems_CategoryId_SortOrder",
                table: "RuleDetails",
                newName: "IX_RuleDetails_RuleId_SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_RuleItems_Code",
                table: "RuleDetails",
                newName: "IX_RuleDetails_Code");

            migrationBuilder.RenameIndex(
                name: "IX_RuleItems_IsActive",
                table: "RuleDetails",
                newName: "IX_RuleDetails_IsActive");

            // Join columns RuleItemId → RuleDetailId
            migrationBuilder.RenameColumn(
                name: "RuleItemId",
                table: "SentenceRules",
                newName: "RuleDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_SentenceRules_RuleItemId",
                table: "SentenceRules",
                newName: "IX_SentenceRules_RuleDetailId");

            migrationBuilder.RenameColumn(
                name: "RuleItemId",
                table: "GradeRules",
                newName: "RuleDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRules_RuleItemId",
                table: "GradeRules",
                newName: "IX_GradeRules_RuleDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_RuleDetails_Rules_RuleId",
                table: "RuleDetails",
                column: "RuleId",
                principalTable: "Rules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeRules_RuleDetails_RuleDetailId",
                table: "GradeRules",
                column: "RuleDetailId",
                principalTable: "RuleDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SentenceRules_RuleDetails_RuleDetailId",
                table: "SentenceRules",
                column: "RuleDetailId",
                principalTable: "RuleDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Align seed naming: Types of Speech (was Direct and Indirect Speech)
            migrationBuilder.Sql("""
                UPDATE Rules
                SET Code = N'TYPES_OF_SPEECH',
                    NameEn = N'Types of Speech',
                    NameUr = N'کلام کی اقسام',
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Code = N'DIRECT_INDIRECT'
                   OR NameEn = N'Direct and Indirect Speech';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeRules_RuleDetails_RuleDetailId",
                table: "GradeRules");

            migrationBuilder.DropForeignKey(
                name: "FK_SentenceRules_RuleDetails_RuleDetailId",
                table: "SentenceRules");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleDetails_Rules_RuleId",
                table: "RuleDetails");

            migrationBuilder.RenameColumn(
                name: "RuleDetailId",
                table: "SentenceRules",
                newName: "RuleItemId");

            migrationBuilder.RenameIndex(
                name: "IX_SentenceRules_RuleDetailId",
                table: "SentenceRules",
                newName: "IX_SentenceRules_RuleItemId");

            migrationBuilder.RenameColumn(
                name: "RuleDetailId",
                table: "GradeRules",
                newName: "RuleItemId");

            migrationBuilder.RenameIndex(
                name: "IX_GradeRules_RuleDetailId",
                table: "GradeRules",
                newName: "IX_GradeRules_RuleItemId");

            migrationBuilder.RenameColumn(
                name: "RuleId",
                table: "RuleDetails",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_RuleDetails_RuleId_SortOrder",
                table: "RuleDetails",
                newName: "IX_RuleItems_CategoryId_SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_RuleDetails_Code",
                table: "RuleDetails",
                newName: "IX_RuleItems_Code");

            migrationBuilder.RenameIndex(
                name: "IX_RuleDetails_IsActive",
                table: "RuleDetails",
                newName: "IX_RuleItems_IsActive");

            migrationBuilder.RenameTable(
                name: "RuleDetails",
                newName: "RuleItems");

            migrationBuilder.RenameIndex(
                name: "IX_Rules_Code",
                table: "Rules",
                newName: "IX_RuleCategories_Code");

            migrationBuilder.RenameIndex(
                name: "IX_Rules_IsActive",
                table: "Rules",
                newName: "IX_RuleCategories_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_Rules_SortOrder",
                table: "Rules",
                newName: "IX_RuleCategories_SortOrder");

            migrationBuilder.RenameTable(
                name: "Rules",
                newName: "RuleCategories");

            migrationBuilder.AddForeignKey(
                name: "FK_RuleItems_RuleCategories_CategoryId",
                table: "RuleItems",
                column: "CategoryId",
                principalTable: "RuleCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

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

            migrationBuilder.Sql("""
                UPDATE RuleCategories
                SET Code = N'DIRECT_INDIRECT',
                    NameEn = N'Direct and Indirect Speech',
                    NameUr = N'براہ راست اور بالواسطہ کلام',
                    UpdatedAtUtc = SYSUTCDATETIME()
                WHERE Code = N'TYPES_OF_SPEECH'
                   OR NameEn = N'Types of Speech';
                """);
        }
    }
}
