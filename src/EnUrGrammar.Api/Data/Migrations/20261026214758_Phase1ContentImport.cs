using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnUrGrammar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase1ContentImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Grades",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityKind = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    ErrorReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviewJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sentences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TextEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TextUr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GradeId = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    MtSuggestedUr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MtSuggestedEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MtConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sentences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sentences_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SentenceRules",
                columns: table => new
                {
                    SentenceId = table.Column<int>(type: "int", nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentenceRules", x => new { x.SentenceId, x.RuleId });
                    table.ForeignKey(
                        name: "FK_SentenceRules_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SentenceRules_Sentences_SentenceId",
                        column: x => x.SentenceId,
                        principalTable: "Sentences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Code",
                table: "Grades",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_UploadedByUserId_CreatedAtUtc",
                table: "ImportBatches",
                columns: new[] { "UploadedByUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SentenceRules_RuleId",
                table: "SentenceRules",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_GradeId_Status_Difficulty",
                table: "Sentences",
                columns: new[] { "GradeId", "Status", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_Sentences_Status",
                table: "Sentences",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "SentenceRules");

            migrationBuilder.DropTable(
                name: "Sentences");

            migrationBuilder.DropIndex(
                name: "IX_Grades_Code",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Grades");
        }
    }
}
