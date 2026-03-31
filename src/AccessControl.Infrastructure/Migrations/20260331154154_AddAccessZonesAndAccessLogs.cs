using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessZonesAndAccessLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_uid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    access_granted = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_zones", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_logs_card_uid",
                table: "access_logs",
                column: "card_uid");

            migrationBuilder.CreateIndex(
                name: "ix_access_logs_device_id",
                table: "access_logs",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_logs_timestamp",
                table: "access_logs",
                column: "timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_access_logs_zone_id",
                table: "access_logs",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_zones_name",
                table: "access_zones",
                column: "name",
                unique: true);

            // Data migration: create AccessZone rows for any zone_ids already referenced by existing cards/devices
            migrationBuilder.Sql("""
                INSERT INTO access_zones (id, name, description, created_at, updated_at)
                SELECT DISTINCT sub.zone_id,
                       'Zone ' || ROW_NUMBER() OVER (ORDER BY sub.zone_id)::text,
                       NULL,
                       NOW(),
                       NOW()
                FROM (
                    SELECT zone_id FROM access_cards WHERE zone_id IS NOT NULL
                    UNION
                    SELECT zone_id FROM devices WHERE zone_id IS NOT NULL
                ) AS sub
                WHERE NOT EXISTS (SELECT 1 FROM access_zones WHERE id = sub.zone_id);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_access_cards_access_zones_zone_id",
                table: "access_cards",
                column: "zone_id",
                principalTable: "access_zones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_devices_access_zones_zone_id",
                table: "devices",
                column: "zone_id",
                principalTable: "access_zones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_access_cards_access_zones_zone_id",
                table: "access_cards");

            migrationBuilder.DropForeignKey(
                name: "fk_devices_access_zones_zone_id",
                table: "devices");

            migrationBuilder.DropTable(
                name: "access_logs");

            migrationBuilder.DropTable(
                name: "access_zones");
        }
    }
}
