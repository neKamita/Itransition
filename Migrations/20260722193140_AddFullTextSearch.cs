using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Itransition.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "UserAttributeValues",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Value" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Positions",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description", "Company", "Level", "Tags" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CandidateProfiles",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeValues_SearchVector",
                table: "UserAttributeValues",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_SearchVector",
                table: "CandidateProfiles",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAttributeValues_SearchVector",
                table: "UserAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_Positions_SearchVector",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_SearchVector",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "UserAttributeValues");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CandidateProfiles");
        }
    }
}
