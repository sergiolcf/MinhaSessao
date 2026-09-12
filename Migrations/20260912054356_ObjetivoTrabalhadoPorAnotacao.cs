using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class ObjetivoTrabalhadoPorAnotacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O scaffold padrão do EF Core tratou isso como um simples RenameColumn (SessaoId ->
            // AnotacaoSessaoId), o que preservaria os valores antigos (Ids de Sessao) numa coluna que
            // passa a apontar pra AnotacoesSessao — quebra a FK/os dados. Por isso a versão manual
            // abaixo: mantém SessaoId até fazer o backfill, só então dropa.
            migrationBuilder.DropForeignKey(
                name: "FK_SessoesObjetivos_Sessoes_SessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.DropIndex(
                name: "IX_SessoesObjetivos_SessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.AddColumn<Guid>(
                name: "AnotacaoSessaoId",
                table: "SessoesObjetivos",
                type: "uuid",
                nullable: true);

            // Backfill: garante que toda Sessão que já tinha SessaoObjetivo tenha ao menos uma
            // AnotacaoSessao pra servir de destino (reaproveita a mais antiga se já existir, ex. o
            // "Anotação inicial" da migration AddAnotacaoSessao; cria uma nova só se a sessão ainda
            // não tiver nenhuma) e então aponta cada SessaoObjetivo pra essa anotação
            migrationBuilder.Sql(@"
                INSERT INTO ""AnotacoesSessao"" (""Id"", ""SessaoId"", ""Titulo"", ""Conteudo"", ""DataRegistro"")
                SELECT gen_random_uuid(), s.""Id"", 'Anotação inicial', '', s.""DataHora""
                FROM ""Sessoes"" s
                WHERE EXISTS (SELECT 1 FROM ""SessoesObjetivos"" so WHERE so.""SessaoId"" = s.""Id"")
                  AND NOT EXISTS (SELECT 1 FROM ""AnotacoesSessao"" a WHERE a.""SessaoId"" = s.""Id"");

                UPDATE ""SessoesObjetivos"" so
                SET ""AnotacaoSessaoId"" = (
                    SELECT a.""Id"" FROM ""AnotacoesSessao"" a
                    WHERE a.""SessaoId"" = so.""SessaoId""
                    ORDER BY a.""DataRegistro"" ASC
                    LIMIT 1
                );
            ");

            migrationBuilder.DropColumn(
                name: "SessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.AlterColumn<Guid>(
                name: "AnotacaoSessaoId",
                table: "SessoesObjetivos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_AnotacaoSessaoId",
                table: "SessoesObjetivos",
                column: "AnotacaoSessaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessoesObjetivos_AnotacoesSessao_AnotacaoSessaoId",
                table: "SessoesObjetivos",
                column: "AnotacaoSessaoId",
                principalTable: "AnotacoesSessao",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessoesObjetivos_AnotacoesSessao_AnotacaoSessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.DropIndex(
                name: "IX_SessoesObjetivos_AnotacaoSessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.AddColumn<Guid>(
                name: "SessaoId",
                table: "SessoesObjetivos",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""SessoesObjetivos"" so
                SET ""SessaoId"" = (SELECT a.""SessaoId"" FROM ""AnotacoesSessao"" a WHERE a.""Id"" = so.""AnotacaoSessaoId"");
            ");

            migrationBuilder.DropColumn(
                name: "AnotacaoSessaoId",
                table: "SessoesObjetivos");

            migrationBuilder.AlterColumn<Guid>(
                name: "SessaoId",
                table: "SessoesObjetivos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_SessaoId",
                table: "SessoesObjetivos",
                column: "SessaoId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessoesObjetivos_Sessoes_SessaoId",
                table: "SessoesObjetivos",
                column: "SessaoId",
                principalTable: "Sessoes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
