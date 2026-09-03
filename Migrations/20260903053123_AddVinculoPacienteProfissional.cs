using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AddVinculoPacienteProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria a tabela de vínculo ANTES de mexer em Pacientes.ProfissionalId, para poder
            // migrar os dados existentes antes de remover a coluna antiga.
            migrationBuilder.CreateTable(
                name: "Vinculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PacienteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataFim = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vinculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vinculos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Vinculos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_PacienteId",
                table: "Vinculos",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_ProfissionalId",
                table: "Vinculos",
                column: "ProfissionalId");

            // Migra cada Paciente existente para um Vínculo Ativo com o ProfissionalId que ele já tinha,
            // usando a DataCadastro do paciente como DataInicio (Status = 0 == StatusVinculo.Ativo).
            // O Id é montado no formato de Guid (lower(hex(...)) com hífens) porque o SQLite não tem gerador de UUID nativo.
            migrationBuilder.Sql(
                @"INSERT INTO Vinculos (Id, PacienteId, ProfissionalId, Status, DataInicio, DataFim)
                  SELECT
                      lower(hex(randomblob(4))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(2))) || '-' || lower(hex(randomblob(6))),
                      Id,
                      ProfissionalId,
                      0,
                      DataCadastro,
                      NULL
                  FROM Pacientes;");

            migrationBuilder.DropForeignKey(
                name: "FK_Pacientes_Profissionais_ProfissionalId",
                table: "Pacientes");

            migrationBuilder.DropIndex(
                name: "IX_Pacientes_ProfissionalId",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "ProfissionalId",
                table: "Pacientes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vinculos");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfissionalId",
                table: "Pacientes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_ProfissionalId",
                table: "Pacientes",
                column: "ProfissionalId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pacientes_Profissionais_ProfissionalId",
                table: "Pacientes",
                column: "ProfissionalId",
                principalTable: "Profissionais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
