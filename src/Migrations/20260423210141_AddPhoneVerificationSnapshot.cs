using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneVerificationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "VerificationRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"VerificationRequests\" vr SET \"PhoneNumber\" = u.\"PhoneNumber\" FROM \"Users\" u WHERE vr.\"UserId\" = u.\"Id\" AND u.\"PhoneNumber\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "VerificationRequests");
        }
    }
}
