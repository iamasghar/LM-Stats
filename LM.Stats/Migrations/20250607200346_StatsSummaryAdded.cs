using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LM.Stats.Migrations
{
    /// <inheritdoc />
    public partial class StatsSummaryAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatsSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatsId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rank = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Might = table.Column<long>(type: "bigint", nullable: false),
                    MightDifference = table.Column<long>(type: "bigint", nullable: false),
                    Kills = table.Column<long>(type: "bigint", nullable: false),
                    KillsDifference = table.Column<long>(type: "bigint", nullable: false),
                    KillsPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EDM = table.Column<long>(type: "bigint", nullable: false),
                    EDMDifference = table.Column<long>(type: "bigint", nullable: false),
                    TroopsLost = table.Column<long>(type: "bigint", nullable: false),
                    TroopsLostDifference = table.Column<long>(type: "bigint", nullable: false),
                    HuntPoints = table.Column<int>(type: "int", nullable: false),
                    HuntPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchasePoints = table.Column<int>(type: "int", nullable: false),
                    PurchasePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FirstHuntTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHuntTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatsSummaries_Stats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "Stats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatsSummaries_StatsId",
                table: "StatsSummaries",
                column: "StatsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatsSummaries");
        }
    }
}
