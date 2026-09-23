using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4RecorrenciasAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assinatura",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Periodicidade = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ProximaCobranca = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MetodoPagamento = table.Column<int>(type: "INTEGER", nullable: false),
                    CartaoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DataCancelamento = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assinatura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assinatura_cartao_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assinatura_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assinatura_conta_ContaId",
                        column: x => x.ContaId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assinatura_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recorrencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Frequencia = table.Column<int>(type: "INTEGER", nullable: false),
                    DataInicio = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataFim = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ProximaOcorrencia = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DiaReferencia = table.Column<int>(type: "INTEGER", nullable: false),
                    GerarAutomaticamente = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValorVariavel = table.Column<bool>(type: "INTEGER", nullable: false),
                    Classificacao = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recorrencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recorrencia_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencia_conta_ContaId",
                        column: x => x.ContaId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recorrencia_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ocorrencia_recorrencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecorrenciaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DataPrevista = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValorPrevistoCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    ValorRealizadoCentavos = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    TransacaoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CompraCartaoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ocorrencia_recorrencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ocorrencia_recorrencia_compra_cartao_CompraCartaoId",
                        column: x => x.CompraCartaoId,
                        principalTable: "compra_cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ocorrencia_recorrencia_recorrencia_RecorrenciaId",
                        column: x => x.RecorrenciaId,
                        principalTable: "recorrencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ocorrencia_recorrencia_transacao_TransacaoId",
                        column: x => x.TransacaoId,
                        principalTable: "transacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_ASSINATURA_ATIVA",
                table: "assinatura",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IDX_ASSINATURA_PROXIMA_COBRANCA",
                table: "assinatura",
                column: "ProximaCobranca");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_CartaoId",
                table: "assinatura",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_CategoriaId",
                table: "assinatura",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_ContaId",
                table: "assinatura",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_assinatura_MembroId",
                table: "assinatura",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_OCORRENCIA_DATA",
                table: "ocorrencia_recorrencia",
                column: "DataPrevista");

            migrationBuilder.CreateIndex(
                name: "IDX_OCORRENCIA_RECORRENCIA",
                table: "ocorrencia_recorrencia",
                column: "RecorrenciaId");

            migrationBuilder.CreateIndex(
                name: "IDX_OCORRENCIA_STATUS",
                table: "ocorrencia_recorrencia",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencia_recorrencia_CompraCartaoId",
                table: "ocorrencia_recorrencia",
                column: "CompraCartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencia_recorrencia_RecorrenciaId_DataPrevista",
                table: "ocorrencia_recorrencia",
                columns: new[] { "RecorrenciaId", "DataPrevista" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencia_recorrencia_TransacaoId",
                table: "ocorrencia_recorrencia",
                column: "TransacaoId");

            migrationBuilder.CreateIndex(
                name: "IDX_RECORRENCIA_PROXIMA",
                table: "recorrencia",
                column: "ProximaOcorrencia");

            migrationBuilder.CreateIndex(
                name: "IDX_RECORRENCIA_STATUS",
                table: "recorrencia",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_recorrencia_CategoriaId",
                table: "recorrencia",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_recorrencia_ContaId",
                table: "recorrencia",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_recorrencia_MembroId",
                table: "recorrencia",
                column: "MembroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assinatura");

            migrationBuilder.DropTable(
                name: "ocorrencia_recorrencia");

            migrationBuilder.DropTable(
                name: "recorrencia");
        }
    }
}
