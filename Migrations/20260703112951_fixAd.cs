using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class fixAd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserActions_action",
                table: "UserActions",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_permission",
                table: "Permissions",
                column: "permission");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserActions_action",
                table: "UserActions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_permission",
                table: "Permissions");
        }
    }
}
