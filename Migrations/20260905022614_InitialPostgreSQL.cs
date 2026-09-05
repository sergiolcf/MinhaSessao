using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinhaSessao.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pacientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeCompleto = table.Column<string>(type: "text", nullable: false),
                    Cpf = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Senha = table.Column<string>(type: "text", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    ContatoEmergencia = table.Column<string>(type: "text", nullable: true),
                    Profissao = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeCompleto = table.Column<string>(type: "text", nullable: false),
                    RegistroCRP = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telefone = table.Column<string>(type: "text", nullable: false),
                    Senha = table.Column<string>(type: "text", nullable: false),
                    Apresentacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AbordagemEspecialidades = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FotoUrl = table.Column<string>(type: "text", nullable: true),
                    DuracaoPadraoSessaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    ValorPadraoConsulta = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnotacoesConfidenciais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: true),
                    Conteudo = table.Column<string>(type: "text", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnotacoesConfidenciais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnotacoesConfidenciais_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnotacoesConfidenciais_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjetivosTerapeuticos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjetivosTerapeuticos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjetivosTerapeuticos_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObjetivosTerapeuticos_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AnotacoesClinicas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessoes_Pacientes_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Pacientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessoes_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vinculos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PacienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Combinados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjetivoTerapeuticoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Concluido = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combinados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Combinados_ObjetivosTerapeuticos_ObjetivoTerapeuticoId",
                        column: x => x.ObjetivoTerapeuticoId,
                        principalTable: "ObjetivosTerapeuticos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessoesObjetivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjetivoTerapeuticoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Observacao = table.Column<string>(type: "text", nullable: true),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_AnotacoesConfidenciais_PacienteId",
                table: "AnotacoesConfidenciais",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AnotacoesConfidenciais_ProfissionalId",
                table: "AnotacoesConfidenciais",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Combinados_ObjetivoTerapeuticoId",
                table: "Combinados",
                column: "ObjetivoTerapeuticoId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetivosTerapeuticos_PacienteId",
                table: "ObjetivosTerapeuticos",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetivosTerapeuticos_ProfissionalId",
                table: "ObjetivosTerapeuticos",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_PacienteId",
                table: "Sessoes",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_ProfissionalId",
                table: "Sessoes",
                column: "ProfissionalId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_ObjetivoTerapeuticoId",
                table: "SessoesObjetivos",
                column: "ObjetivoTerapeuticoId");

            migrationBuilder.CreateIndex(
                name: "IX_SessoesObjetivos_SessaoId",
                table: "SessoesObjetivos",
                column: "SessaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_PacienteId",
                table: "Vinculos",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Vinculos_ProfissionalId",
                table: "Vinculos",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnotacoesConfidenciais");

            migrationBuilder.DropTable(
                name: "Combinados");

            migrationBuilder.DropTable(
                name: "SessoesObjetivos");

            migrationBuilder.DropTable(
                name: "Vinculos");

            migrationBuilder.DropTable(
                name: "ObjetivosTerapeuticos");

            migrationBuilder.DropTable(
                name: "Sessoes");

            migrationBuilder.DropTable(
                name: "Pacientes");

            migrationBuilder.DropTable(
                name: "Profissionais");
        }
    }
}
