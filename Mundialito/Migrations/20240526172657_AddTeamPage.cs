using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mundialito.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamPage",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TournamentTeamId",
                table: "Teams",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamPage",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "TournamentTeamId",
                table: "Teams");
        }
    }
}
