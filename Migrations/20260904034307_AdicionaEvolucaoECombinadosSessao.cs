using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaEvolucaoECombinadosSessao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados");

            migrationBuilder.AddColumn<string>(
                name: "AnotacoesClinicas",
                table: "Sessoes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjetivoId",
                table: "Combinados",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados",
                column: "ObjetivoId",
                principalTable: "Objetivos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Combinados_Objetivos_ObjetivoId",
                table: "Combinados");

            migrationBuilder.DropColumn(
                name: "AnotacoesClinicas",
                table: "Sessoes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjetivoId",
                table: "Combinados",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
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
    }
}
