using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardholdersAndAccessProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_access_cards_access_zones_zone_id",
                table: "access_cards");

            migrationBuilder.DropIndex(
                name: "ix_access_cards_user_id",
                table: "access_cards");

            migrationBuilder.DropIndex(
                name: "ix_access_cards_zone_id",
                table: "access_cards");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "access_cards");

            migrationBuilder.DropColumn(
                name: "zone_id",
                table: "access_cards");

            migrationBuilder.AddColumn<Guid>(
                name: "cardholder_id",
                table: "access_cards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "access_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cardholders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cardholders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_profile_zones",
                columns: table => new
                {
                    access_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_zone_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_profile_zones", x => new { x.access_profile_id, x.access_zone_id });
                    table.ForeignKey(
                        name: "fk_access_profile_zones_access_profiles_access_profile_id",
                        column: x => x.access_profile_id,
                        principalTable: "access_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_access_profile_zones_access_zones_access_zone_id",
                        column: x => x.access_zone_id,
                        principalTable: "access_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cardholder_access_profile",
                columns: table => new
                {
                    access_profiles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cardholders_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cardholder_access_profile", x => new { x.access_profiles_id, x.cardholders_id });
                    table.ForeignKey(
                        name: "fk_cardholder_access_profile_access_profiles_access_profiles_id",
                        column: x => x.access_profiles_id,
                        principalTable: "access_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cardholder_access_profile_cardholders_cardholders_id",
                        column: x => x.cardholders_id,
                        principalTable: "cardholders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_cardholder_id",
                table: "access_cards",
                column: "cardholder_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_profile_zones_access_zone_id",
                table: "access_profile_zones",
                column: "access_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_profiles_name",
                table: "access_profiles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cardholder_access_profile_cardholders_id",
                table: "cardholder_access_profile",
                column: "cardholders_id");

            migrationBuilder.AddForeignKey(
                name: "fk_access_cards_cardholders_cardholder_id",
                table: "access_cards",
                column: "cardholder_id",
                principalTable: "cardholders",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_access_cards_cardholders_cardholder_id",
                table: "access_cards");

            migrationBuilder.DropTable(
                name: "access_profile_zones");

            migrationBuilder.DropTable(
                name: "cardholder_access_profile");

            migrationBuilder.DropTable(
                name: "access_profiles");

            migrationBuilder.DropTable(
                name: "cardholders");

            migrationBuilder.DropIndex(
                name: "ix_access_cards_cardholder_id",
                table: "access_cards");

            migrationBuilder.DropColumn(
                name: "cardholder_id",
                table: "access_cards");

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "access_cards",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "zone_id",
                table: "access_cards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_user_id",
                table: "access_cards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_cards_zone_id",
                table: "access_cards",
                column: "zone_id");

            migrationBuilder.AddForeignKey(
                name: "fk_access_cards_access_zones_zone_id",
                table: "access_cards",
                column: "zone_id",
                principalTable: "access_zones",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
