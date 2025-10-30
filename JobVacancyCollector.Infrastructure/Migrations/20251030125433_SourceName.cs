using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobVacancyCollector.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SourceName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceName",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceName",
                table: "Vacancies");
        }
    }
}
