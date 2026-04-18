using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TsLaser.Crm.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderToIntakeSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "intake_submissions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gender",
                table: "intake_submissions");
        }
    }
}
