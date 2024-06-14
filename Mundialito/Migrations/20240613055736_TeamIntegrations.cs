﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mundialito.Migrations
{
    /// <inheritdoc />
    public partial class TeamIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationsData",
                table: "Teams",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IntegrationsData",
                table: "Teams");
        }
    }
}
