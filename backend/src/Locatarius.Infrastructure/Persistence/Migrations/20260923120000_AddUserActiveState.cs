using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Locatarius.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActiveState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");
        }
    }
}
