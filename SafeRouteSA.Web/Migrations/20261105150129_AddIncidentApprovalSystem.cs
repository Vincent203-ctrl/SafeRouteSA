using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeRouteSA.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentApprovalSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "Incidents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAtUtc",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedByUserId",
                table: "Incidents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Incidents");
        }
    }
}
