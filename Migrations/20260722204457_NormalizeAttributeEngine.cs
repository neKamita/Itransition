using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Itransition.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeAttributeEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_SearchVector",
                table: "CandidateProfiles");

            migrationBuilder.AddColumn<string>(
                name: "BuiltInKey",
                table: "AttributeDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "AttributeDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBuiltIn",
                table: "AttributeDefinitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAt",
                table: "AttributeDefinitions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttributeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeCategories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AttributeCategories",
                columns: new[] { "Id", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("c1000000-0000-0000-0000-000000000001"), "Personal Information", 10 },
                    { new Guid("c1000000-0000-0000-0000-000000000002"), "Certification", 20 },
                    { new Guid("c1000000-0000-0000-0000-000000000003"), "Domain Knowledge", 30 },
                    { new Guid("c1000000-0000-0000-0000-000000000004"), "Soft Skills", 40 },
                    { new Guid("c1000000-0000-0000-0000-000000000005"), "Language", 50 },
                    { new Guid("c1000000-0000-0000-0000-000000000006"), "Professional", 60 },
                    { new Guid("c1000000-0000-0000-0000-000000000007"), "Technical", 70 },
                    { new Guid("c1000000-0000-0000-0000-000000000008"), "Other", 999 }
                });

            migrationBuilder.Sql(
                """
                UPDATE "AttributeDefinitions"
                SET "CategoryId" = CASE LOWER(TRIM("Category"))
                    WHEN 'personal information' THEN 'c1000000-0000-0000-0000-000000000001'::uuid
                    WHEN 'certification' THEN 'c1000000-0000-0000-0000-000000000002'::uuid
                    WHEN 'domain knowledge' THEN 'c1000000-0000-0000-0000-000000000003'::uuid
                    WHEN 'soft skills' THEN 'c1000000-0000-0000-0000-000000000004'::uuid
                    WHEN 'language' THEN 'c1000000-0000-0000-0000-000000000005'::uuid
                    WHEN 'professional' THEN 'c1000000-0000-0000-0000-000000000006'::uuid
                    WHEN 'technical' THEN 'c1000000-0000-0000-0000-000000000007'::uuid
                    ELSE 'c1000000-0000-0000-0000-000000000008'::uuid
                END;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "AttributeDefinitions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "AttributeDefinitions");

            migrationBuilder.InsertData(
                table: "AttributeDefinitions",
                columns: new[] { "Id", "BuiltInKey", "CategoryId", "DataType", "Description", "IsBuiltIn", "LastUsedAt", "Name" },
                values: new object[,]
                {
                    { new Guid("b1000000-0000-0000-0000-000000000001"), "first_name", new Guid("c1000000-0000-0000-0000-000000000001"), 0, "Required profile field shared with position templates.", true, null, "First Name" },
                    { new Guid("b1000000-0000-0000-0000-000000000002"), "last_name", new Guid("c1000000-0000-0000-0000-000000000001"), 0, "Required profile field shared with position templates.", true, null, "Last Name" },
                    { new Guid("b1000000-0000-0000-0000-000000000003"), "location", new Guid("c1000000-0000-0000-0000-000000000001"), 0, "Required profile field shared with position templates.", true, null, "Location" },
                    { new Guid("b1000000-0000-0000-0000-000000000004"), "personal_photo", new Guid("c1000000-0000-0000-0000-000000000001"), 2, "Required profile field shared with position templates.", true, null, "Personal Photo" }
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "UserAttributeValues" ("Id", "CandidateProfileId", "AttributeDefinitionId", "Value")
                SELECT (md5(profile."Id"::text || ':first_name'))::uuid,
                       profile."Id",
                       'b1000000-0000-0000-0000-000000000001'::uuid,
                       NULLIF(TRIM(profile."FirstName"), '')
                FROM "CandidateProfiles" AS profile
                ON CONFLICT ("CandidateProfileId", "AttributeDefinitionId") DO NOTHING;

                INSERT INTO "UserAttributeValues" ("Id", "CandidateProfileId", "AttributeDefinitionId", "Value")
                SELECT (md5(profile."Id"::text || ':last_name'))::uuid,
                       profile."Id",
                       'b1000000-0000-0000-0000-000000000002'::uuid,
                       NULLIF(TRIM(profile."LastName"), '')
                FROM "CandidateProfiles" AS profile
                ON CONFLICT ("CandidateProfileId", "AttributeDefinitionId") DO NOTHING;

                INSERT INTO "UserAttributeValues" ("Id", "CandidateProfileId", "AttributeDefinitionId", "Value")
                SELECT (md5(profile."Id"::text || ':location'))::uuid,
                       profile."Id",
                       'b1000000-0000-0000-0000-000000000003'::uuid,
                       NULLIF(TRIM(profile."Location"), '')
                FROM "CandidateProfiles" AS profile
                ON CONFLICT ("CandidateProfileId", "AttributeDefinitionId") DO NOTHING;

                INSERT INTO "UserAttributeValues" ("Id", "CandidateProfileId", "AttributeDefinitionId", "Value")
                SELECT (md5(profile."Id"::text || ':personal_photo'))::uuid,
                       profile."Id",
                       'b1000000-0000-0000-0000-000000000004'::uuid,
                       NULLIF(TRIM(profile."PersonalPhotoUrl"), '')
                FROM "CandidateProfiles" AS profile
                ON CONFLICT ("CandidateProfileId", "AttributeDefinitionId") DO NOTHING;
                """);

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "PersonalPhotoUrl",
                table: "CandidateProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_BuiltInKey",
                table: "AttributeDefinitions",
                column: "BuiltInKey",
                unique: true,
                filter: "\"BuiltInKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_CategoryId",
                table: "AttributeDefinitions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeCategories_Name",
                table: "AttributeCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeDefinitions_AttributeCategories_CategoryId",
                table: "AttributeDefinitions",
                column: "CategoryId",
                principalTable: "AttributeCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeDefinitions_AttributeCategories_CategoryId",
                table: "AttributeDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "CandidateProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "CandidateProfiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalPhotoUrl",
                table: "CandidateProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CandidateProfiles",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "FirstName", "LastName", "Location" });

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "AttributeDefinitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "AttributeDefinitions" AS definition
                SET "Category" = category."Name"
                FROM "AttributeCategories" AS category
                WHERE category."Id" = definition."CategoryId";

                UPDATE "CandidateProfiles" AS profile
                SET "FirstName" = COALESCE(value."Value", '')
                FROM "UserAttributeValues" AS value
                WHERE value."CandidateProfileId" = profile."Id"
                  AND value."AttributeDefinitionId" = 'b1000000-0000-0000-0000-000000000001'::uuid;

                UPDATE "CandidateProfiles" AS profile
                SET "LastName" = COALESCE(value."Value", '')
                FROM "UserAttributeValues" AS value
                WHERE value."CandidateProfileId" = profile."Id"
                  AND value."AttributeDefinitionId" = 'b1000000-0000-0000-0000-000000000002'::uuid;

                UPDATE "CandidateProfiles" AS profile
                SET "Location" = value."Value"
                FROM "UserAttributeValues" AS value
                WHERE value."CandidateProfileId" = profile."Id"
                  AND value."AttributeDefinitionId" = 'b1000000-0000-0000-0000-000000000003'::uuid;

                UPDATE "CandidateProfiles" AS profile
                SET "PersonalPhotoUrl" = value."Value"
                FROM "UserAttributeValues" AS value
                WHERE value."CandidateProfileId" = profile."Id"
                  AND value."AttributeDefinitionId" = 'b1000000-0000-0000-0000-000000000004'::uuid;
                """);

            migrationBuilder.DropIndex(
                name: "IX_AttributeDefinitions_BuiltInKey",
                table: "AttributeDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_AttributeDefinitions_CategoryId",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "BuiltInKey",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "IsBuiltIn",
                table: "AttributeDefinitions");

            migrationBuilder.DropColumn(
                name: "LastUsedAt",
                table: "AttributeDefinitions");

            migrationBuilder.DropTable(
                name: "AttributeCategories");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_SearchVector",
                table: "CandidateProfiles",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
