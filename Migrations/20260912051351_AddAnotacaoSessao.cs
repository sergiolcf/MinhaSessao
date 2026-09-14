using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AddAnotacaoSessao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A ordem importa: cria a tabela nova ANTES de mexer na coluna antiga, faz o backfill
            // dos dados existentes (uma anotação por sessão que já tinha texto) e só depois derruba
            // a coluna — mesmo espírito de AddVinculoPacienteProfissional (ver CLAUDE.md), senão os
            // dados são perdidos antes de serem copiados
            migrationBuilder.CreateTable(
                name: "AnotacoesSessao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnotacoesSessao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnotacoesSessao_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnotacoesSessao_SessaoId",
                table: "AnotacoesSessao",
                column: "SessaoId");

            // Backfill: toda sessão que já tinha texto em AnotacoesClinicas vira uma anotação com
            // título genérico "Anotação inicial" (gen_random_uuid() é nativo do Postgres desde a
            // versão 13, sem precisar da extensão pgcrypto)
            migrationBuilder.Sql(@"
                INSERT INTO ""AnotacoesSessao"" (""Id"", ""SessaoId"", ""Titulo"", ""Conteudo"", ""DataRegistro"")
                SELECT gen_random_uuid(), ""Id"", 'Anotação inicial', ""AnotacoesClinicas"", ""DataHora""
                FROM ""Sessoes""
                WHERE ""AnotacoesClinicas"" IS NOT NULL AND TRIM(""AnotacoesClinicas"") <> '';
            ");

            migrationBuilder.DropColumn(
                name: "AnotacoesClinicas",
                table: "Sessoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnotacoesSessao");

            migrationBuilder.AddColumn<string>(
                name: "AnotacoesClinicas",
                table: "Sessoes",
                type: "text",
                nullable: true);
        }
    }
}
