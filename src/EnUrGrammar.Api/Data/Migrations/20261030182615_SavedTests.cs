using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnUrGrammar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SavedTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SavedTestId",
                table: "PracticeSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Layout = table.Column<byte>(type: "tinyint", nullable: false),
                    DropdownMode = table.Column<byte>(type: "tinyint", nullable: false),
                    ExerciseTypesJson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GradeIdsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RuleIdsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Difficulty = table.Column<byte>(type: "tinyint", nullable: true),
                    SourceLanguage = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedTests_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_SavedTestId",
                table: "PracticeSessions",
                column: "SavedTestId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedTests_CreatedByUserId_CreatedAtUtc",
                table: "SavedTests",
                columns: new[] { "CreatedByUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedTests_IsActive",
                table: "SavedTests",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeSessions_SavedTests_SavedTestId",
                table: "PracticeSessions",
                column: "SavedTestId",
                principalTable: "SavedTests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeSessions_SavedTests_SavedTestId",
                table: "PracticeSessions");

            migrationBuilder.DropTable(
                name: "SavedTests");

            migrationBuilder.DropIndex(
                name: "IX_PracticeSessions_SavedTestId",
                table: "PracticeSessions");

            migrationBuilder.DropColumn(
                name: "SavedTestId",
                table: "PracticeSessions");
        }
    }
}
