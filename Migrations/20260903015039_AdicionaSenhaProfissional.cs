using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using MinhaSessao.Models.Entities;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSenhaProfissional : Migration
    {
        // Senha temporária atribuída aos cadastros que existiam antes desta migration
        private const string SenhaTemporaria = "MinhaSessao@123";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Senha",
                table: "Profissionais",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Cadastros antigos ficaram com Senha = "" (defaultValue acima); atribui um hash de senha temporária a eles
            var hasher = new PasswordHasher<Profissional>();
            var hashSenhaTemporaria = hasher.HashPassword(new Profissional(), SenhaTemporaria).Replace("'", "''");

            migrationBuilder.Sql($"UPDATE Profissionais SET Senha = '{hashSenhaTemporaria}' WHERE Senha = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Senha",
                table: "Profissionais");
        }
    }
}
