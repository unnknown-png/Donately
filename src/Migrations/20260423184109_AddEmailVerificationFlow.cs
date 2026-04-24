using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VerificationRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotStarted",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pending");

            migrationBuilder.AlterColumn<Guid>(
                name: "AttachmentId",
                table: "VerificationRequests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "VerificationRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationTokenExpiresAt",
                table: "VerificationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationTokenHash",
                table: "VerificationRequests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmedAt",
                table: "VerificationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneNumberConfirmedAt",
                table: "VerificationRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_Email",
                table: "VerificationRequests",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_Email",
                table: "VerificationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "VerificationRequests");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenExpiresAt",
                table: "VerificationRequests");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationTokenHash",
                table: "VerificationRequests");

            migrationBuilder.DropColumn(
                name: "EmailConfirmedAt",
                table: "VerificationRequests");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmedAt",
                table: "VerificationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "VerificationRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "NotStarted");

            migrationBuilder.AlterColumn<Guid>(
                name: "AttachmentId",
                table: "VerificationRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationRequests_UserId",
                table: "VerificationRequests",
                column: "UserId");
        }
    }
}
