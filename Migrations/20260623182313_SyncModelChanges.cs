using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "jwttoken",
                table: "users",
                newName: "JwtToken");

            migrationBuilder.CreateIndex(
                name: "IX_users_id",
                table: "users",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_provideruserid",
                table: "users",
                column: "provideruserid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_login",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_provideruserid",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "JwtToken",
                table: "users",
                newName: "jwttoken");
        }
    }
}
