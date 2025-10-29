using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobVacancyCollector.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NewMigrationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyNote",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Conditions",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameContact",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Salary",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SalaryNote",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "CompanyNote",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Conditions",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "NameContact",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "SalaryNote",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Vacancies");
        }
    }
}
