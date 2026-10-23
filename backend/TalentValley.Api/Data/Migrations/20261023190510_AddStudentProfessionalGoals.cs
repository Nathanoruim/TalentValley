using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TalentValley.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentProfessionalGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CargoDesejado",
                table: "Alunos",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjetivoProfissional",
                table: "Alunos",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoDesejado",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "ObjetivoProfissional",
                table: "Alunos");
        }
    }
}
