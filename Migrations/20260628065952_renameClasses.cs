using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MyApiBlya.Migrations
{
    /// <inheritdoc />
    public partial class renameClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_permissions_permissions_perms_id",
                table: "users_permissions");

            migrationBuilder.DropTable(
                name: "UsersHistory");

            migrationBuilder.DropTable(
                name: "histories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_permissions",
                table: "permissions");

            migrationBuilder.RenameTable(
                name: "permissions",
                newName: "Permissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions",
                column: "id");

            migrationBuilder.CreateTable(
                name: "UserActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserActionHistories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActionHistories", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserActionHistories_UserActions_action",
                        column: x => x.action,
                        principalTable: "UserActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserActionHistories_users_user",
                        column: x => x.user,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActionHistories_action",
                table: "UserActionHistories",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_UserActionHistories_user",
                table: "UserActionHistories",
                column: "user");

            migrationBuilder.AddForeignKey(
                name: "FK_users_permissions_Permissions_perms_id",
                table: "users_permissions",
                column: "perms_id",
                principalTable: "Permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_permissions_Permissions_perms_id",
                table: "users_permissions");

            migrationBuilder.DropTable(
                name: "UserActionHistories");

            migrationBuilder.DropTable(
                name: "UserActions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "permissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_permissions",
                table: "permissions",
                column: "id");

            migrationBuilder.CreateTable(
                name: "histories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_histories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsersHistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    action = table.Column<int>(type: "integer", nullable: false),
                    user = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_UsersHistory_histories_action",
                        column: x => x.action,
                        principalTable: "histories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsersHistory_users_user",
                        column: x => x.user,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsersHistory_action",
                table: "UsersHistory",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_UsersHistory_user",
                table: "UsersHistory",
                column: "user");

            migrationBuilder.AddForeignKey(
                name: "FK_users_permissions_permissions_perms_id",
                table: "users_permissions",
                column: "perms_id",
                principalTable: "permissions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
