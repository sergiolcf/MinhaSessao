using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AddSessaoObjetivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessoesObjetivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjetivoTerapeuticoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true),
                    DataRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessoesObjetivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessoesObjetivos_ObjetivosTerapeuticos_ObjetivoTerapeuticoId",
                        column: x => x.ObjetivoTerapeuticoId,
                        principalTable: "ObjetivosTerapeuticos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessoesObjetivos_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_ObjetivoTerapeuticoId",
                table: "SessoesObjetivos",
                column: "ObjetivoTerapeuticoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_SessaoId",
                table: "SessoesObjetivos",
                column: "SessaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessoesObjetivos");
        }
    }
}
