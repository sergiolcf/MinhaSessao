using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaResponsavelPaciente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpfResponsavel",
                table: "Pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NecessitaResponsavel",
                table: "Pacientes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NomeResponsavel",
                table: "Pacientes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefoneResponsavel",
                table: "Pacientes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpfResponsavel",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NecessitaResponsavel",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "NomeResponsavel",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "TelefoneResponsavel",
                table: "Pacientes");
        }
    }
}
