using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    [Migration("20260622223000_FixUserIsBlockedDefault")]
    public partial class FixUserIsBlockedDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "isblocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.Sql("""
                UPDATE users
                SET isblocked = FALSE
                WHERE login = 'admin';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "isblocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
