using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessCardsAndDeviceConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "configuration",
                table: "devices",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateTable(
                name: "access_cards",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_uid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_cards", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_card_uid",
                table: "access_cards",
                column: "card_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_user_id",
                table: "access_cards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_zone_id",
                table: "access_cards",
                column: "zone_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_cards");

            migrationBuilder.DropColumn(
                name: "configuration",
                table: "devices");
        }
    }
}
