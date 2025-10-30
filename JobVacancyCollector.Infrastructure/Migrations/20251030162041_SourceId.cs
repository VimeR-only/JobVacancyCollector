using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobVacancyCollector.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SourceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Vacancies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Vacancies");
        }
    }
}
