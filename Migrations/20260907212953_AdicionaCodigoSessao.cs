using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaCodigoSessao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Sessoes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            // Backfill retroativo: numera as sessões já existentes agrupando por Profissional (ordem
            // cronológica pela DataHora, mais antiga primeiro) e monta o Codigo no formato
            // "MS_{numero}_{iniciais}" — TRANSLATE remove os acentos comuns do PT-BR do nome do
            // profissional sem depender da extensão "unaccent" do Postgres (que exigiria permissão
            // de superusuário, indisponível em bancos gerenciados como o Neon)
            migrationBuilder.Sql(@"
                WITH profissional_iniciais AS (
                    SELECT
                        p.""Id"" AS profissional_id,
                        UPPER(LEFT(SPLIT_PART(nome.normalizado, ' ', 1), 1))
                        || UPPER(LEFT(SPLIT_PART(nome.normalizado, ' ', GREATEST(1, ARRAY_LENGTH(REGEXP_SPLIT_TO_ARRAY(nome.normalizado, '\s+'), 1))), 1)) AS iniciais
                    FROM ""Profissionais"" p
                    CROSS JOIN LATERAL (
                        SELECT TRIM(TRANSLATE(
                            p.""NomeCompleto"",
                            'áàâãäÁÀÂÃÄéèêëÉÈÊËíìîïÍÌÎÏóòôõöÓÒÔÕÖúùûüÚÙÛÜçÇñÑ',
                            'aaaaaAAAAAeeeeEEEEiiiiIIIIoooooOOOOOuuuuUUUUcCnN'
                        )) AS normalizado
                    ) AS nome
                ),
                sessoes_numeradas AS (
                    SELECT
                        ""Id"" AS sessao_id,
                        ""ProfissionalId"" AS profissional_id,
                        ROW_NUMBER() OVER (PARTITION BY ""ProfissionalId"" ORDER BY ""DataHora"" ASC, ""Id"" ASC) AS numero
                    FROM ""Sessoes""
                )
                UPDATE ""Sessoes"" s
                SET ""Codigo"" = 'MS_' || sn.numero || '_' || pi.iniciais
                FROM sessoes_numeradas sn
                JOIN profissional_iniciais pi ON pi.profissional_id = sn.profissional_id
                WHERE s.""Id"" = sn.sessao_id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Sessoes");
        }
    }
}
