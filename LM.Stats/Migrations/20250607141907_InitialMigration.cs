using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LM.Stats.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UniqueIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hunts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total = table.Column<int>(type: "int", nullable: false),
                    HuntCount = table.Column<int>(type: "int", nullable: false),
                    Purchase = table.Column<int>(type: "int", nullable: false),
                    L1Hunt = table.Column<int>(type: "int", nullable: false),
                    L2Hunt = table.Column<int>(type: "int", nullable: false),
                    L3Hunt = table.Column<int>(type: "int", nullable: false),
                    L4Hunt = table.Column<int>(type: "int", nullable: false),
                    L5Hunt = table.Column<int>(type: "int", nullable: false),
                    L1Purchase = table.Column<int>(type: "int", nullable: false),
                    L2Purchase = table.Column<int>(type: "int", nullable: false),
                    L3Purchase = table.Column<int>(type: "int", nullable: false),
                    L4Purchase = table.Column<int>(type: "int", nullable: false),
                    L5Purchase = table.Column<int>(type: "int", nullable: false),
                    PointsHunt = table.Column<int>(type: "int", nullable: false),
                    GoalPercentageHunt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointsPurchase = table.Column<int>(type: "int", nullable: false),
                    GoalPercentagePurchase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstHuntTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHuntTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hunts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hunts_Stats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "Stats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IggId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rank = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Might = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    OldMight = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    MightDifference = table.Column<long>(type: "bigint", nullable: false),
                    Kills = table.Column<long>(type: "bigint", nullable: false),
                    OldKills = table.Column<long>(type: "bigint", nullable: false),
                    KillsDifference = table.Column<long>(type: "bigint", nullable: false),
                    OldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kills_Stats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "Stats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtherStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Might = table.Column<long>(type: "bigint", nullable: false),
                    Kills = table.Column<long>(type: "bigint", nullable: false),
                    TroopsKilled = table.Column<long>(type: "bigint", nullable: false),
                    EnemiesDestroyedMight = table.Column<long>(type: "bigint", nullable: false),
                    TroopsLost = table.Column<long>(type: "bigint", nullable: false),
                    WinRate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatsId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherStats_Stats_StatsId",
                        column: x => x.StatsId,
                        principalTable: "Stats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Hunts_StatsId",
                table: "Hunts",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_Kills_StatsId",
                table: "Kills",
                column: "StatsId");

            migrationBuilder.CreateIndex(
                name: "IX_OtherStats_StatsId",
                table: "OtherStats",
                column: "StatsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "Hunts");

            migrationBuilder.DropTable(
                name: "Kills");

            migrationBuilder.DropTable(
                name: "OtherStats");

            migrationBuilder.DropTable(
                name: "Stats");
        }
    }
}
