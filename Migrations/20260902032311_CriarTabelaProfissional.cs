using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class CriarTabelaProfissional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeCompleto = table.Column<string>(type: "TEXT", nullable: false),
                    RegistroCRP = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", nullable: false),
                    Apresentacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FotoUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Profissionais");
        }
    }
}
