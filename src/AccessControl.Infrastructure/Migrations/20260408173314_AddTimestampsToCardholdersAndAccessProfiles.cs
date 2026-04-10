using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTimestampsToCardholdersAndAccessProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow;

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "cardholders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: now);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "cardholders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: now);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "access_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: now);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "access_profiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: now);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "cardholders");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "cardholders");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "access_profiles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "access_profiles");
        }
    }
}
