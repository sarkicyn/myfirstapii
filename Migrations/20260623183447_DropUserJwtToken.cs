using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class DropUserJwtToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JwtToken",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JwtToken",
                table: "users",
                type: "text",
                nullable: true);
        }
    }
}
