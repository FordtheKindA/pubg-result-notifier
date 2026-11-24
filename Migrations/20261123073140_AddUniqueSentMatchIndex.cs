using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePractice.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueSentMatchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SentMatches_TournamentId_MatchId",
                table: "SentMatches",
                columns: new[] { "TournamentId", "MatchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SentMatches_TournamentId_MatchId",
                table: "SentMatches");
        }
    }
}
