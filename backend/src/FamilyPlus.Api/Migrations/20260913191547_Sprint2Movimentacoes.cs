using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2Movimentacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Operacao = table.Column<int>(type: "INTEGER", nullable: false),
                    DadosAnteriores = table.Column<string>(type: "TEXT", nullable: true),
                    DadosNovos = table.Column<string>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Data = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auditoria_usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "transferencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaOrigemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaDestinoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transferencia_conta_ContaDestinoId",
                        column: x => x.ContaDestinoId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencia_conta_ContaOrigemId",
                        column: x => x.ContaOrigemId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transferencia_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    DataCompetencia = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataMovimentacao = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    TransferenciaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transacao_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transacao_conta_ContaId",
                        column: x => x.ContaId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transacao_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transacao_transferencia_TransferenciaId",
                        column: x => x.TransferenciaId,
                        principalTable: "transferencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_Data",
                table: "auditoria",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_Entidade_EntidadeId",
                table: "auditoria",
                columns: new[] { "Entidade", "EntidadeId" });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_UsuarioId",
                table: "auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IDX_TRANSACAO_CATEGORIA",
                table: "transacao",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IDX_TRANSACAO_CONTA",
                table: "transacao",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IDX_TRANSACAO_DATA",
                table: "transacao",
                column: "DataMovimentacao");

            migrationBuilder.CreateIndex(
                name: "IDX_TRANSACAO_MEMBRO",
                table: "transacao",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_TRANSACAO_STATUS",
                table: "transacao",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transacao_TransferenciaId",
                table: "transacao",
                column: "TransferenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_ContaDestinoId",
                table: "transferencia",
                column: "ContaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_ContaOrigemId",
                table: "transferencia",
                column: "ContaOrigemId");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_Data",
                table: "transferencia",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_transferencia_MembroId",
                table: "transferencia",
                column: "MembroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria");

            migrationBuilder.DropTable(
                name: "transacao");

            migrationBuilder.DropTable(
                name: "transferencia");
        }
    }
}
