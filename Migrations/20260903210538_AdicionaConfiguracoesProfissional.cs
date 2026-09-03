using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaConfiguracoesProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbordagemEspecialidades",
                table: "Profissionais",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuracaoPadraoSessaoMinutos",
                table: "Profissionais",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPadraoConsulta",
                table: "Profissionais",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbordagemEspecialidades",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "DuracaoPadraoSessaoMinutos",
                table: "Profissionais");

            migrationBuilder.DropColumn(
                name: "ValorPadraoConsulta",
                table: "Profissionais");
        }
    }
}
