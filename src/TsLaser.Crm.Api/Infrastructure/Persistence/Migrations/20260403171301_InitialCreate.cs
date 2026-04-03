using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsLaser.Crm.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Contacts = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Terms = table.Column<string>(type: "TEXT", nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    birth_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    Gender = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    referral_partner_id = table.Column<int>(type: "INTEGER", nullable: true),
                    referral_custom = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "active"),
                    stopped_reason = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clients_partners_referral_partner_id",
                        column: x => x.referral_partner_id,
                        principalTable: "partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tattoos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    removal_zone = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    corrections_count = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    last_pigment_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    last_laser_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    no_laser_before = table.Column<bool>(type: "INTEGER", nullable: false),
                    previous_removal_place = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    desired_result = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tattoos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tattoos_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "intake_submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tattoo_id = table.Column<int>(type: "INTEGER", nullable: true),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    referral_source = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    tattoo_type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    tattoo_age = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    corrections_info = table.Column<string>(type: "TEXT", nullable: true),
                    previous_removal_info = table.Column<string>(type: "TEXT", nullable: true),
                    previous_removal_where = table.Column<string>(type: "TEXT", nullable: true),
                    desired_result = table.Column<string>(type: "TEXT", nullable: true),
                    source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "landing"),
                    is_new_client = table.Column<bool>(type: "INTEGER", nullable: false),
                    raw_payload = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intake_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_intake_submissions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_intake_submissions_tattoos_tattoo_id",
                        column: x => x.tattoo_id,
                        principalTable: "tattoos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "laser_sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    client_id = table.Column<int>(type: "INTEGER", nullable: false),
                    tattoo_id = table.Column<int>(type: "INTEGER", nullable: true),
                    tattoo_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    session_number = table.Column<int>(type: "INTEGER", nullable: true),
                    sub_session = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    wavelength = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    diameter = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    density = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    hertz = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    flashes_count = table.Column<int>(type: "INTEGER", nullable: true),
                    session_date = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    break_period = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laser_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laser_sessions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_laser_sessions_tattoos_tattoo_id",
                        column: x => x.tattoo_id,
                        principalTable: "tattoos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_clients_name",
                table: "clients",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_clients_phone",
                table: "clients",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_clients_referral_partner_id",
                table: "clients",
                column: "referral_partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_client_id",
                table: "intake_submissions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_phone",
                table: "intake_submissions",
                column: "phone");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_tattoo_id",
                table: "intake_submissions",
                column: "tattoo_id");

            migrationBuilder.CreateIndex(
                name: "ix_laser_sessions_client_id",
                table: "laser_sessions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_laser_sessions_tattoo_id",
                table: "laser_sessions",
                column: "tattoo_id");

            migrationBuilder.CreateIndex(
                name: "ix_partners_name",
                table: "partners",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_tattoos_client_id",
                table: "tattoos",
                column: "client_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intake_submissions");

            migrationBuilder.DropTable(
                name: "laser_sessions");

            migrationBuilder.DropTable(
                name: "tattoos");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "partners");
        }
    }
}
