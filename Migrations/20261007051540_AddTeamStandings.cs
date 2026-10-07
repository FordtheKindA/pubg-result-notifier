using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServicePractice.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamStandings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<string>(type: "TEXT", nullable: false),
                    TeamTag = table.Column<string>(type: "TEXT", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalKills = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalKillPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPlacementPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AdvancementPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPoints = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStandings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamStandings");
        }
    }
}
