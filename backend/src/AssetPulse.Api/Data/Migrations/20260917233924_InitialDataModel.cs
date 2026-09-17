using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetPulse.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Pressure = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.CheckConstraint("CK_Assets_AssetCode_Uppercase", "[AssetCode] COLLATE Latin1_General_100_BIN2 = UPPER([AssetCode]) COLLATE Latin1_General_100_BIN2");
                    table.CheckConstraint("CK_Assets_Pressure_Nonnegative", "[Pressure] IS NULL OR [Pressure] >= CONVERT(decimal(10, 2), 0)");
                    table.CheckConstraint("CK_Assets_RequiredText_Trimmed", "LEN([Name]) > 0 AND [Name] = LTRIM(RTRIM([Name])) AND LEN([AssetCode]) > 0 AND [AssetCode] = LTRIM(RTRIM([AssetCode])) AND LEN([Type]) > 0 AND [Type] = LTRIM(RTRIM([Type])) AND LEN([Location]) > 0 AND [Location] = LTRIM(RTRIM([Location]))");
                    table.CheckConstraint("CK_Assets_Temperature_AbsoluteZero", "[Temperature] IS NULL OR [Temperature] >= CONVERT(decimal(10, 2), -273.15)");
                });

            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.CheckConstraint("CK_Alarms_Message_Trimmed", "LEN([Message]) > 0 AND [Message] = LTRIM(RTRIM([Message]))");
                    table.CheckConstraint("CK_Alarms_Severity", "[Severity] IN ('Warning', 'Critical')");
                    table.CheckConstraint("CK_Alarms_Status", "[Status] IN ('Active', 'Acknowledged', 'Resolved')");
                    table.CheckConstraint("CK_Alarms_Timestamps", "([Status] = 'Active' AND [AcknowledgedAt] IS NULL AND [ResolvedAt] IS NULL) OR ([Status] = 'Acknowledged' AND [AcknowledgedAt] IS NOT NULL AND [ResolvedAt] IS NULL) OR ([Status] = 'Resolved' AND [ResolvedAt] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Alarms_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEvents", x => x.Id);
                    table.CheckConstraint("CK_AssetEvents_Description_Trimmed", "LEN([Description]) > 0 AND [Description] = LTRIM(RTRIM([Description]))");
                    table.CheckConstraint("CK_AssetEvents_NewStatus", "[NewStatus] IN ('Healthy', 'Warning', 'Critical')");
                    table.CheckConstraint("CK_AssetEvents_PreviousStatus", "[PreviousStatus] IS NULL OR [PreviousStatus] IN ('Healthy', 'Warning', 'Critical')");
                    table.ForeignKey(
                        name: "FK_AssetEvents_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_AssetId_Status",
                table: "Alarms",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_Status_Severity",
                table: "Alarms",
                columns: new[] { "Status", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetId_Timestamp",
                table: "AssetEvents",
                columns: new[] { "AssetId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetCode",
                table: "Assets",
                column: "AssetCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");

            migrationBuilder.DropTable(
                name: "AssetEvents");

            migrationBuilder.DropTable(
                name: "Assets");
        }
    }
}
