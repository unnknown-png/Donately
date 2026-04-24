using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations
{
    /// <inheritdoc />
    public partial class UpdateVerificationWorkflowSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"VerificationRequests\" SET \"Status\" = 'NotStarted' WHERE \"Status\" = 'Pending';");

            migrationBuilder.Sql("UPDATE \"VerificationRequests\" SET \"Status\" = 'InProgress' WHERE \"EmailConfirmedAt\" IS NOT NULL AND \"Status\" IN ('InReview', 'NotStarted');");

            migrationBuilder.Sql("UPDATE \"Users\" u SET \"VerificationStatus\" = 1 FROM \"VerificationRequests\" vr WHERE vr.\"UserId\" = u.\"Id\" AND vr.\"EmailConfirmedAt\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"VerificationRequests\" SET \"Status\" = 'Pending' WHERE \"Status\" = 'NotStarted';");

            migrationBuilder.Sql("UPDATE \"VerificationRequests\" SET \"Status\" = 'InReview' WHERE \"EmailConfirmedAt\" IS NOT NULL AND \"Status\" = 'InProgress';");

            migrationBuilder.Sql("UPDATE \"Users\" u SET \"VerificationStatus\" = 0 FROM \"VerificationRequests\" vr WHERE vr.\"UserId\" = u.\"Id\" AND vr.\"EmailConfirmedAt\" IS NOT NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
