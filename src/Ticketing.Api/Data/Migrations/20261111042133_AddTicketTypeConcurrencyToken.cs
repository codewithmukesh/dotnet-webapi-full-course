using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261111042133_AddTicketTypeConcurrencyToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<uint>(
            name: "xmin",
            table: "ticket_types",
            type: "xid",
            rowVersion: true,
            nullable: false,
            defaultValue: 0u);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "xmin",
            table: "ticket_types");
    }
}
