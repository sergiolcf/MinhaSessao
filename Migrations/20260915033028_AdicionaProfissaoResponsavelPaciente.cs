using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaProfissaoResponsavelPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfissaoResponsavel",
                table: "Pacientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfissaoResponsavel",
                table: "Pacientes");
        }
    }
}
