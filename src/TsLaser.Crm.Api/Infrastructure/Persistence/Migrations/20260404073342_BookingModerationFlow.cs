using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsLaser.Crm.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BookingModerationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_intake_submissions_clients_client_id",
                table: "intake_submissions");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "intake_submissions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "approved_client_id",
                table: "intake_submissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "approved_tattoo_id",
                table: "intake_submissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "intake_submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "intake_submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reviewed_by",
                table: "intake_submissions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "intake_submissions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "pending");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_approved_client_id",
                table: "intake_submissions",
                column: "approved_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_approved_tattoo_id",
                table: "intake_submissions",
                column: "approved_tattoo_id");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_created_at",
                table: "intake_submissions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_intake_submissions_status",
                table: "intake_submissions",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "FK_intake_submissions_clients_client_id",
                table: "intake_submissions",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_intake_submissions_clients_client_id",
                table: "intake_submissions");

            migrationBuilder.DropIndex(
                name: "ix_intake_submissions_approved_client_id",
                table: "intake_submissions");

            migrationBuilder.DropIndex(
                name: "ix_intake_submissions_approved_tattoo_id",
                table: "intake_submissions");

            migrationBuilder.DropIndex(
                name: "ix_intake_submissions_created_at",
                table: "intake_submissions");

            migrationBuilder.DropIndex(
                name: "ix_intake_submissions_status",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "approved_client_id",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "approved_tattoo_id",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "reviewed_by",
                table: "intake_submissions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "intake_submissions");

            migrationBuilder.AlterColumn<int>(
                name: "client_id",
                table: "intake_submissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_intake_submissions_clients_client_id",
                table: "intake_submissions",
                column: "client_id",
                principalTable: "clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
