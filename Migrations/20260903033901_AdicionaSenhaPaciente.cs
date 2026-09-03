using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using MinhaSessao.Models.Entities;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaSenhaPaciente : Migration
    {
        // Senha temporária atribuída aos pacientes que existiam antes desta migration
        private const string SenhaTemporaria = "MinhaSessao@123";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Senha",
                table: "Pacientes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Pacientes antigos ficaram com Senha = "" (defaultValue acima); atribui um hash de senha temporária a eles
            var hasher = new PasswordHasher<Paciente>();
            var hashSenhaTemporaria = hasher.HashPassword(new Paciente(), SenhaTemporaria).Replace("'", "''");

            migrationBuilder.Sql($"UPDATE Pacientes SET Senha = '{hashSenhaTemporaria}' WHERE Senha = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Senha",
                table: "Pacientes");
        }
    }
}
