using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsBlocked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isblocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isblocked",
                table: "users");
        }
    }
}
