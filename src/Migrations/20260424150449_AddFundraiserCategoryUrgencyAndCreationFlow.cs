using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Donately.Migrations
{
    /// <inheritdoc />
    public partial class AddFundraiserCategoryUrgencyAndCreationFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Fundraisers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Support");

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "Fundraisers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Fundraisers_Category",
                table: "Fundraisers",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Fundraisers_Category",
                table: "Fundraisers");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Fundraisers");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "Fundraisers");
        }
    }
}
