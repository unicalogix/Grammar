using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnUrGrammar.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase2aPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SourceLanguage = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    TargetLanguage = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PracticeSessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SentenceId = table.Column<int>(type: "int", nullable: false),
                    ExerciseType = table.Column<byte>(type: "tinyint", nullable: false),
                    PromptLanguage = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    ExpectedAnswer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StudentAnswer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NormalizedExpected = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NormalizedStudent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attempts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attempts_PracticeSessions_PracticeSessionId",
                        column: x => x.PracticeSessionId,
                        principalTable: "PracticeSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attempts_Sentences_SentenceId",
                        column: x => x.SentenceId,
                        principalTable: "Sentences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_PracticeSessionId_SequenceNo",
                table: "Attempts",
                columns: new[] { "PracticeSessionId", "SequenceNo" });

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_SentenceId",
                table: "Attempts",
                column: "SentenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_AnsweredAtUtc",
                table: "Attempts",
                columns: new[] { "UserId", "AnsweredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeSessions_UserId_StartedAtUtc",
                table: "PracticeSessions",
                columns: new[] { "UserId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attempts");

            migrationBuilder.DropTable(
                name: "PracticeSessions");
        }
    }
}
