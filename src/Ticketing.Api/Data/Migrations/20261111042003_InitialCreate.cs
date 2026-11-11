using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261111042003_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Venue_City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Venue_Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ticket_types",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                QuantityLeft = table.Column<int>(type: "integer", nullable: false),
                MaxPerUser = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ticket_types", x => x.Id);
                table.ForeignKey(
                    name: "FK_ticket_types_events_EventId",
                    column: x => x.EventId,
                    principalTable: "events",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "bookings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bookings", x => x.Id);
                table.ForeignKey(
                    name: "FK_bookings_ticket_types_TicketTypeId",
                    column: x => x.TicketTypeId,
                    principalTable: "ticket_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_bookings_TicketTypeId",
            table: "bookings",
            column: "TicketTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_bookings_UserId_TicketTypeId",
            table: "bookings",
            columns: new[] { "UserId", "TicketTypeId" });

        migrationBuilder.CreateIndex(
            name: "IX_events_StartsAt_Id",
            table: "events",
            columns: new[] { "StartsAt", "Id" });

        migrationBuilder.CreateIndex(
            name: "IX_ticket_types_EventId",
            table: "ticket_types",
            column: "EventId");
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
