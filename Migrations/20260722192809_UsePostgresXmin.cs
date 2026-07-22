using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itransition.Migrations
{
    /// <inheritdoc />
    public partial class UsePostgresXmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserAttributeValues");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProjectProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AttributeDefinitions");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "UserAttributeValues",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ProjectProfiles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Positions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PositionAttributes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PositionAccessRules",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Cvs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CandidateProfiles",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AttributeOptions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AttributeDefinitions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "UserAttributeValues");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ProjectProfiles");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PositionAttributes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PositionAccessRules");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AttributeOptions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AttributeDefinitions");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserAttributeValues",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProjectProfiles",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Positions",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Cvs",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CandidateProfiles",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AttributeDefinitions",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
