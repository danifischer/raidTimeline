using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace raidTimeline.Database.Migrations
{
    public partial class Migration00 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bosses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FightId = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bosses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurenceStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OccurenceEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Killed = table.Column<bool>(type: "boolean", nullable: false),
                    EncounterTime = table.Column<string>(type: "text", nullable: false),
                    HitPointsRemaining = table.Column<double>(type: "double precision", nullable: false),
                    BossForeignKey = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encounters_Bosses_BossForeignKey",
                        column: x => x.BossForeignKey,
                        principalTable: "Bosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EncounterPlayer",
                columns: table => new
                {
                    EncountersId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayersId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterPlayer", x => new { x.EncountersId, x.PlayersId });
                    table.ForeignKey(
                        name: "FK_EncounterPlayer_Encounters_EncountersId",
                        column: x => x.EncountersId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EncounterPlayer_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneralStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Damage = table.Column<long>(type: "bigint", nullable: false),
                    Dps = table.Column<long>(type: "bigint", nullable: false),
                    Cc = table.Column<long>(type: "bigint", nullable: false),
                    ResTime = table.Column<double>(type: "double precision", nullable: false),
                    ResAmount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneralStatistics_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GeneralStatistics_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterPlayer_PlayersId",
                table: "EncounterPlayer",
                column: "PlayersId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_BossForeignKey",
                table: "Encounters",
                column: "BossForeignKey");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralStatistics_EncounterId",
                table: "GeneralStatistics",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneralStatistics_PlayerId",
                table: "GeneralStatistics",
                column: "PlayerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncounterPlayer");

            migrationBuilder.DropTable(
                name: "GeneralStatistics");

            migrationBuilder.DropTable(
                name: "Encounters");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Bosses");
        }
    }
}
