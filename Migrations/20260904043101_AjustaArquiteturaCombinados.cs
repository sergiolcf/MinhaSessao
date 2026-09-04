using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AjustaArquiteturaCombinados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria a tabela de execuções ANTES de mexer em Combinados, para poder migrar os dados
            // existentes (SessaoId + Status de cada Combinado já vinculado a uma sessão) antes de remover essas colunas.
            migrationBuilder.CreateTable(
                name: "SessaoCombinados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CombinadoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessaoCombinados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessaoCombinados_Combinados_CombinadoId",
                        column: x => x.CombinadoId,
                        principalTable: "Combinados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessaoCombinados_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessaoCombinados_CombinadoId",
                table: "SessaoCombinados",
                column: "CombinadoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessaoCombinados_SessaoId",
                table: "SessaoCombinados",
                column: "SessaoId");

            // Migra cada Combinado já vinculado a uma Sessão (SessaoId preenchido) para uma Execução
            // (SessaoCombinado), preservando o Status/DataCriacao originais como Status/DataRegistro da checagem.
            // O Id é montado no mesmo formato hexadecimal maiúsculo (upper(hex(...))) já usado pelo EF Core
            // para colunas Guid neste banco, já que o SQLite não tem gerador de UUID nativo.
            migrationBuilder.Sql(
                @"INSERT INTO SessaoCombinados (Id, SessaoId, CombinadoId, Status, DataRegistro)
                  SELECT
                      upper(hex(randomblob(4))) || '-' || upper(hex(randomblob(2))) || '-' || upper(hex(randomblob(2))) || '-' || upper(hex(randomblob(2))) || '-' || upper(hex(randomblob(6))),
                      SessaoId,
                      Id,
                      Status,
                      DataCriacao
                  FROM Combinados
                  WHERE SessaoId IS NOT NULL AND ObjetivoId IS NOT NULL;");

            // Remove os Combinados "Geral" (sem Objetivo) — o novo modelo exige ObjetivoId em todo Combinado,
            // e esses registros não têm um Objetivo válido para onde migrar.
            migrationBuilder.Sql("DELETE FROM Combinados WHERE ObjetivoId IS NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados");

            migrationBuilder.DropForeignKey(
                name: "FK_Combinados_Sessoes_SessaoId",
                table: "Combinados");

            migrationBuilder.DropIndex(
                name: "IX_Combinados_SessaoId",
                table: "Combinados");

            migrationBuilder.DropColumn(
                name: "SessaoId",
                table: "Combinados");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Combinados");

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjetivoId",
                table: "Combinados",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados",
                column: "ObjetivoId",
                principalTable: "Objetivos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados");

            migrationBuilder.DropTable(
                name: "SessaoCombinados");

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjetivoId",
                table: "Combinados",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "SessaoId",
                table: "Combinados",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Combinados",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Combinados_SessaoId",
                table: "Combinados",
                column: "SessaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados",
                column: "ObjetivoId",
                principalTable: "Objetivos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Combinados_Sessoes_SessaoId",
                table: "Combinados",
                column: "SessaoId",
                principalTable: "Sessoes",
                principalColumn: "Id");
        }
    }
}
