using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Api.Data.Migrations;

/// <inheritdoc />
public partial class _20261125170808_AddUsersAndAuth : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "organizer_id",
            table: "events",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                replaced_by_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_events_organizer_id",
            table: "events",
            column: "organizer_id");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token_hash",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id",
            table: "refresh_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "fk_bookings_users_user_id",
            table: "bookings",
            column: "user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "fk_events_users_organizer_id",
            table: "events",
            column: "organizer_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_bookings_users_user_id",
            table: "bookings");

        migrationBuilder.DropForeignKey(
            name: "fk_events_users_organizer_id",
            table: "events");

        migrationBuilder.DropTable(
            name: "refresh_tokens");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropIndex(
            name: "ix_events_organizer_id",
            table: "events");

        migrationBuilder.DropColumn(
            name: "organizer_id",
            table: "events");
    }
}
