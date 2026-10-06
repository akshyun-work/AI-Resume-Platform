using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResumeAnalysis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobStructuredJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StructuredJson",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StructuredJson",
                table: "Jobs");
        }
    }
}
