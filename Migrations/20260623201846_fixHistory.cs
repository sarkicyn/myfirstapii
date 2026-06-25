using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class fixHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersHistory",
                table: "UsersHistory");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "UsersHistory",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "createdat",
                table: "UsersHistory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersHistory",
                table: "UsersHistory",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_UsersHistory_action",
                table: "UsersHistory",
                column: "action");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UsersHistory",
                table: "UsersHistory");

            migrationBuilder.DropIndex(
                name: "IX_UsersHistory_action",
                table: "UsersHistory");

            migrationBuilder.DropColumn(
                name: "id",
                table: "UsersHistory");

            migrationBuilder.DropColumn(
                name: "createdat",
                table: "UsersHistory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UsersHistory",
                table: "UsersHistory",
                columns: new[] { "action", "user" });
        }
    }
}
