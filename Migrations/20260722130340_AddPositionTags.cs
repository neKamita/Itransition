using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itransition.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Positions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Positions");
        }
    }
}
