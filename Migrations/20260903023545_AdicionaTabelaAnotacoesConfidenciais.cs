using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTabelaAnotacoesConfidenciais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnotacoesConfidenciais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PacienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: true),
                    Conteudo = table.Column<string>(type: "TEXT", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnotacoesConfidenciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnotacoesConfidenciais_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnotacoesConfidenciais_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnotacoesConfidenciais_PacienteId",
                table: "AnotacoesConfidenciais",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AnotacoesConfidenciais_ProfissionalId",
                table: "AnotacoesConfidenciais",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnotacoesConfidenciais");
        }
    }
}
