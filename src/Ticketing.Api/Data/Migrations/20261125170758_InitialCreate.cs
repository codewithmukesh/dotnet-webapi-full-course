using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261125170758_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                venue_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                venue_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_events", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "ticket_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                total_quantity = table.Column<int>(type: "integer", nullable: false),
                quantity_left = table.Column<int>(type: "integer", nullable: false),
                max_per_user = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ticket_types", x => x.id);
                table.ForeignKey(
                    name: "fk_ticket_types_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                ticket_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_bookings", x => x.id);
                table.ForeignKey(
                    name: "fk_bookings_ticket_types_ticket_type_id",
                    column: x => x.ticket_type_id,
                    principalTable: "ticket_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_bookings_ticket_type_id",
            table: "bookings",
            column: "ticket_type_id");

        migrationBuilder.CreateIndex(
            name: "ix_bookings_user_id_ticket_type_id",
            table: "bookings",
            columns: new[] { "user_id", "ticket_type_id" });

        migrationBuilder.CreateIndex(
            name: "ix_events_starts_at_id",
            table: "events",
            columns: new[] { "starts_at", "id" });

        migrationBuilder.CreateIndex(
            name: "ix_ticket_types_event_id",
            table: "ticket_types",
            column: "event_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bookings");

        migrationBuilder.DropTable(
            name: "ticket_types");

        migrationBuilder.DropTable(
            name: "events");
    }
}
