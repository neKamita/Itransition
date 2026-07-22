using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Itransition.Migrations
{
    /// <inheritdoc />
    public partial class HardenRoleFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAttributeValues_Cvs_CvId",
                table: "UserAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_UserAttributeValues_CandidateProfileId",
                table: "UserAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_UserAttributeValues_CvId",
                table: "UserAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_PositionAttributes_PositionId",
                table: "PositionAttributes");

            migrationBuilder.DropIndex(
                name: "IX_PositionAccessRules_PositionId",
                table: "PositionAccessRules");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_CandidateProfileId",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AttributeOptions_AttributeDefinitionId",
                table: "AttributeOptions");

            migrationBuilder.DropColumn(
                name: "CvId",
                table: "UserAttributeValues");

            migrationBuilder.DropColumn(
                name: "DislikesCount",
                table: "Cvs");

            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Cvs");

            migrationBuilder.CreateTable(
                name: "CvLikes",
                columns: table => new
                {
                    CvId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecruiterId = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvLikes", x => new { x.CvId, x.RecruiterId });
                    table.ForeignKey(
                        name: "FK_CvLikes_AspNetUsers_RecruiterId",
                        column: x => x.RecruiterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvLikes_Cvs_CvId",
                        column: x => x.CvId,
                        principalTable: "Cvs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeValues_CandidateProfileId_AttributeDefinitionId",
                table: "UserAttributeValues",
                columns: new[] { "CandidateProfileId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionAttributes_PositionId_AttributeDefinitionId",
                table: "PositionAttributes",
                columns: new[] { "PositionId", "AttributeDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionAccessRules_PositionId_AttributeDefinitionId_Operat~",
                table: "PositionAccessRules",
                columns: new[] { "PositionId", "AttributeDefinitionId", "Operator", "TargetValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_CandidateProfileId_PositionId",
                table: "Cvs",
                columns: new[] { "CandidateProfileId", "PositionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeDefinitionId_Value",
                table: "AttributeOptions",
                columns: new[] { "AttributeDefinitionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttributeDefinitions_Name",
                table: "AttributeDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvLikes_RecruiterId",
                table: "CvLikes",
                column: "RecruiterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvLikes");

            migrationBuilder.DropIndex(
                name: "IX_UserAttributeValues_CandidateProfileId_AttributeDefinitionId",
                table: "UserAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_PositionAttributes_PositionId_AttributeDefinitionId",
                table: "PositionAttributes");

            migrationBuilder.DropIndex(
                name: "IX_PositionAccessRules_PositionId_AttributeDefinitionId_Operat~",
                table: "PositionAccessRules");

            migrationBuilder.DropIndex(
                name: "IX_Cvs_CandidateProfileId_PositionId",
                table: "Cvs");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AttributeOptions_AttributeDefinitionId_Value",
                table: "AttributeOptions");

            migrationBuilder.DropIndex(
                name: "IX_AttributeDefinitions_Name",
                table: "AttributeDefinitions");

            migrationBuilder.AddColumn<Guid>(
                name: "CvId",
                table: "UserAttributeValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DislikesCount",
                table: "Cvs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Cvs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeValues_CandidateProfileId",
                table: "UserAttributeValues",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAttributeValues_CvId",
                table: "UserAttributeValues",
                column: "CvId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAttributes_PositionId",
                table: "PositionAttributes",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionAccessRules_PositionId",
                table: "PositionAccessRules",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Cvs_CandidateProfileId",
                table: "Cvs",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_UserId",
                table: "CandidateProfiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeOptions_AttributeDefinitionId",
                table: "AttributeOptions",
                column: "AttributeDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAttributeValues_Cvs_CvId",
                table: "UserAttributeValues",
                column: "CvId",
                principalTable: "Cvs",
                principalColumn: "Id");
        }
    }
}
