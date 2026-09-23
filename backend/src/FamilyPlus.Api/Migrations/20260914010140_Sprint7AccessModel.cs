using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7AccessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "usuario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MembroId",
                table: "usuario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UltimoAcessoEm",
                table: "usuario",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "transferencia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "transacao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "recorrencia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "parcela_cartao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "orcamento",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "ocorrencia_recorrencia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataNascimento",
                table: "membro",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "membro",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "membro",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "item_orcamento",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "fatura",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "estorno_cartao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "conta",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "compra_cartao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "categoria",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "cartao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "assinatura",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FamiliaId",
                table: "alerta",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "conexao_financeira",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Instituicao = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Configuracao = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conexao_financeira", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "configuracao_notificacao",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AvisarContasProximas = table.Column<bool>(type: "INTEGER", nullable: false),
                    AntecedenciaDias = table.Column<int>(type: "INTEGER", nullable: false),
                    AvisarFatura = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvisarSaldoProjetadoNegativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    AvisarOrcamento = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_notificacao", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_configuracao_notificacao_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "familia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    MoedaPadrao = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_familia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "importacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NomeArquivo = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalItens = table.Column<int>(type: "INTEGER", nullable: false),
                    ItensValidos = table.Column<int>(type: "INTEGER", nullable: false),
                    ItensComErro = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcluidaEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "perfil_acesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Sistema = table.Column<bool>(type: "INTEGER", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_acesso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Modulo = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissao_escopo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Recurso = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TipoEscopo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissao_escopo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_permissao_escopo_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "preferencia_usuario",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Idioma = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FormatoData = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PaginaInicial = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ItensPorPagina = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfirmarOperacoes = table.Column<bool>(type: "INTEGER", nullable: false),
                    PeriodoPadraoDashboard = table.Column<string>(type: "TEXT", nullable: false),
                    Tema = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preferencia_usuario", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_preferencia_usuario_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rateio_despesa",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransacaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ValorDespesaCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    PadraoAplicado = table.Column<bool>(type: "INTEGER", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rateio_despesa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rateio_despesa_transacao_TransacaoId",
                        column: x => x.TransacaoId,
                        principalTable: "transacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "configuracao_familia",
                columns: table => new
                {
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Moeda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Locale = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PrimeiroDiaSemana = table.Column<int>(type: "INTEGER", nullable: false),
                    InicioMesFinanceiro = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_familia", x => x.FamiliaId);
                    table.ForeignKey(
                        name: "FK_configuracao_familia_familia_FamiliaId",
                        column: x => x.FamiliaId,
                        principalTable: "familia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "importacao_arquivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportacaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacao_arquivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_importacao_arquivo_importacao_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "importacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "importacao_item",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImportacaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Linha = table.Column<int>(type: "INTEGER", nullable: false),
                    Dados = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    HashDedupe = table.Column<string>(type: "TEXT", nullable: true),
                    Valido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Erro = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_importacao_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_importacao_item_importacao_ImportacaoId",
                        column: x => x.ImportacaoId,
                        principalTable: "importacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_perfil",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PerfilAcessoId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_perfil", x => new { x.UsuarioId, x.PerfilAcessoId });
                    table.ForeignKey(
                        name: "FK_usuario_perfil_perfil_acesso_PerfilAcessoId",
                        column: x => x.PerfilAcessoId,
                        principalTable: "perfil_acesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_perfil_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "perfil_permissao",
                columns: table => new
                {
                    PerfilAcessoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PermissaoId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_perfil_permissao", x => new { x.PerfilAcessoId, x.PermissaoId });
                    table.ForeignKey(
                        name: "FK_perfil_permissao_perfil_acesso_PerfilAcessoId",
                        column: x => x.PerfilAcessoId,
                        principalTable: "perfil_acesso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_perfil_permissao_permissao_PermissaoId",
                        column: x => x.PermissaoId,
                        principalTable: "permissao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissao_membro",
                columns: table => new
                {
                    PermissaoEscopoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissao_membro", x => new { x.PermissaoEscopoId, x.MembroId });
                    table.ForeignKey(
                        name: "FK_permissao_membro_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_permissao_membro_permissao_escopo_PermissaoEscopoId",
                        column: x => x.PermissaoEscopoId,
                        principalTable: "permissao_escopo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rateio_despesa_item",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RateioDespesaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Percentual = table.Column<decimal>(type: "TEXT", precision: 12, scale: 4, nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rateio_despesa_item", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rateio_despesa_item_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rateio_despesa_item_rateio_despesa_RateioDespesaId",
                        column: x => x.RateioDespesaId,
                        principalTable: "rateio_despesa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuario_MembroId",
                table: "usuario",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_membro_FamiliaId",
                table: "membro",
                column: "FamiliaId");

            migrationBuilder.CreateIndex(
                name: "IX_importacao_arquivo_ImportacaoId",
                table: "importacao_arquivo",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_importacao_item_ImportacaoId",
                table: "importacao_item",
                column: "ImportacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_perfil_acesso_FamiliaId_Nome",
                table: "perfil_acesso",
                columns: new[] { "FamiliaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_perfil_permissao_PermissaoId",
                table: "perfil_permissao",
                column: "PermissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_permissao_FamiliaId_Codigo",
                table: "permissao",
                columns: new[] { "FamiliaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissao_escopo_UsuarioId_Recurso",
                table: "permissao_escopo",
                columns: new[] { "UsuarioId", "Recurso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissao_membro_MembroId",
                table: "permissao_membro",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_rateio_despesa_TransacaoId",
                table: "rateio_despesa",
                column: "TransacaoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rateio_despesa_item_MembroId",
                table: "rateio_despesa_item",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_rateio_despesa_item_RateioDespesaId",
                table: "rateio_despesa_item",
                column: "RateioDespesaId");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_perfil_PerfilAcessoId",
                table: "usuario_perfil",
                column: "PerfilAcessoId");

            migrationBuilder.AddForeignKey(
                name: "FK_membro_familia_FamiliaId",
                table: "membro",
                column: "FamiliaId",
                principalTable: "familia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_usuario_membro_MembroId",
                table: "usuario",
                column: "MembroId",
                principalTable: "membro",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_membro_familia_FamiliaId",
                table: "membro");

            migrationBuilder.DropForeignKey(
                name: "FK_usuario_membro_MembroId",
                table: "usuario");

            migrationBuilder.DropTable(
                name: "conexao_financeira");

            migrationBuilder.DropTable(
                name: "configuracao_familia");

            migrationBuilder.DropTable(
                name: "configuracao_notificacao");

            migrationBuilder.DropTable(
                name: "importacao_arquivo");

            migrationBuilder.DropTable(
                name: "importacao_item");

            migrationBuilder.DropTable(
                name: "perfil_permissao");

            migrationBuilder.DropTable(
                name: "permissao_membro");

            migrationBuilder.DropTable(
                name: "preferencia_usuario");

            migrationBuilder.DropTable(
                name: "rateio_despesa_item");

            migrationBuilder.DropTable(
                name: "usuario_perfil");

            migrationBuilder.DropTable(
                name: "familia");

            migrationBuilder.DropTable(
                name: "importacao");

            migrationBuilder.DropTable(
                name: "permissao");

            migrationBuilder.DropTable(
                name: "permissao_escopo");

            migrationBuilder.DropTable(
                name: "rateio_despesa");

            migrationBuilder.DropTable(
                name: "perfil_acesso");

            migrationBuilder.DropIndex(
                name: "IX_usuario_MembroId",
                table: "usuario");

            migrationBuilder.DropIndex(
                name: "IX_membro_FamiliaId",
                table: "membro");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "MembroId",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "UltimoAcessoEm",
                table: "usuario");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "transferencia");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "transacao");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "recorrencia");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "parcela_cartao");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "orcamento");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "ocorrencia_recorrencia");

            migrationBuilder.DropColumn(
                name: "DataNascimento",
                table: "membro");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "membro");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "membro");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "item_orcamento");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "fatura");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "estorno_cartao");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "conta");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "compra_cartao");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "categoria");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "cartao");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "assinatura");

            migrationBuilder.DropColumn(
                name: "FamiliaId",
                table: "alerta");
        }
    }
}
