using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerFinder.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ServiceCategory = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    MainServices = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ContactPerson = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    ContactTitle = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    LinkedIn = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataCenterExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmartHandsCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImacCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    BreakFixCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    NetworkSupportCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    ServerSupportCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    StorageSupportCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    MicrosoftPartnerStatus = table.Column<string>(type: "TEXT", nullable: false),
                    DellPartnerStatus = table.Column<string>(type: "TEXT", nullable: false),
                    CiscoPartnerStatus = table.Column<string>(type: "TEXT", nullable: false),
                    HpePartnerStatus = table.Column<string>(type: "TEXT", nullable: false),
                    Certifications = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AiServerBuildCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    GpuWorkstationBuildCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    EdgeAiDeploymentCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalLlmDeploymentCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    NvidiaGpuExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    AmdGpuExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    NvidiaJetsonExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmallAiClusterExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnPremAiDeploymentExperience = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiModelInferenceSetup = table.Column<bool>(type: "INTEGER", nullable: false),
                    LinuxDockerKubernetesCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    CoolingPowerPlanningCapability = table.Column<bool>(type: "INTEGER", nullable: false),
                    AiInfrastructureSummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AiSummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    QualificationScore = table.Column<int>(type: "INTEGER", nullable: false),
                    AiInfrastructureScore = table.Column<int>(type: "INTEGER", nullable: false),
                    RecommendedLevel = table.Column<string>(type: "TEXT", nullable: false),
                    ManualReviewStatus = table.Column<string>(type: "TEXT", nullable: false),
                    FollowUpAction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_CompanyName",
                table: "Partners",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_Country",
                table: "Partners",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_Partners_RecommendedLevel",
                table: "Partners",
                column: "RecommendedLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partners");
        }
    }
}
