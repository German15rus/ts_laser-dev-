using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsLaser.Crm.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    intake_submission_id = table.Column<int>(type: "INTEGER", nullable: false),
                    master_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    start_time = table.Column<DateTime>(type: "TEXT", nullable: false),
                    duration_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    appointment_status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "waiting"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointments_intake_submissions_intake_submission_id",
                        column: x => x.intake_submission_id,
                        principalTable: "intake_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_appointments_intake_submission_id",
                table: "appointments",
                column: "intake_submission_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_start_time",
                table: "appointments",
                column: "start_time");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "appointments");
        }
    }
}
