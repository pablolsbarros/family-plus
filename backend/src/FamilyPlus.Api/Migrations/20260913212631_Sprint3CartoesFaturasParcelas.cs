using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3CartoesFaturasParcelas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Origem",
                table: "transacao",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cartao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Bandeira = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    UltimosDigitos = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    LimiteTotalCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    DiaFechamento = table.Column<int>(type: "INTEGER", nullable: false),
                    DiaVencimento = table.Column<int>(type: "INTEGER", nullable: false),
                    ContaPagamentoPadraoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cartao_conta_ContaPagamentoPadraoId",
                        column: x => x.ContaPagamentoPadraoId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cartao_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compra_cartao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CartaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValorTotalCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "INTEGER", nullable: false),
                    DataCompra = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compra_cartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compra_cartao_cartao_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compra_cartao_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compra_cartao_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fatura",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CartaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Competencia = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataFechamento = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataVencimento = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ValorTotalCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    DataPagamento = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ContaPagamentoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fatura", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fatura_cartao_CartaoId",
                        column: x => x.CartaoId,
                        principalTable: "cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fatura_conta_ContaPagamentoId",
                        column: x => x.ContaPagamentoId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estorno_cartao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompraCartaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Data = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estorno_cartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_estorno_cartao_compra_cartao_CompraCartaoId",
                        column: x => x.CompraCartaoId,
                        principalTable: "compra_cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_estorno_cartao_fatura_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parcela_cartao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompraCartaoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroParcela = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantidadeTotal = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    DataCompetencia = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parcela_cartao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parcela_cartao_compra_cartao_CompraCartaoId",
                        column: x => x.CompraCartaoId,
                        principalTable: "compra_cartao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parcela_cartao_fatura_FaturaId",
                        column: x => x.FaturaId,
                        principalTable: "fatura",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cartao_ContaPagamentoPadraoId",
                table: "cartao",
                column: "ContaPagamentoPadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_cartao_MembroId",
                table: "cartao",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_COMPRA_CARTAO",
                table: "compra_cartao",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IDX_COMPRA_DATA",
                table: "compra_cartao",
                column: "DataCompra");

            migrationBuilder.CreateIndex(
                name: "IX_compra_cartao_CategoriaId",
                table: "compra_cartao",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_compra_cartao_MembroId",
                table: "compra_cartao",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IX_estorno_cartao_CompraCartaoId",
                table: "estorno_cartao",
                column: "CompraCartaoId");

            migrationBuilder.CreateIndex(
                name: "IX_estorno_cartao_FaturaId",
                table: "estorno_cartao",
                column: "FaturaId");

            migrationBuilder.CreateIndex(
                name: "IDX_FATURA_CARTAO",
                table: "fatura",
                column: "CartaoId");

            migrationBuilder.CreateIndex(
                name: "IDX_FATURA_COMPETENCIA",
                table: "fatura",
                column: "Competencia");

            migrationBuilder.CreateIndex(
                name: "IDX_FATURA_STATUS",
                table: "fatura",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_fatura_CartaoId_Competencia",
                table: "fatura",
                columns: new[] { "CartaoId", "Competencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_ContaPagamentoId",
                table: "fatura",
                column: "ContaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IDX_PARCELA_COMPETENCIA",
                table: "parcela_cartao",
                column: "DataCompetencia");

            migrationBuilder.CreateIndex(
                name: "IDX_PARCELA_COMPRA",
                table: "parcela_cartao",
                column: "CompraCartaoId");

            migrationBuilder.CreateIndex(
                name: "IDX_PARCELA_FATURA",
                table: "parcela_cartao",
                column: "FaturaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estorno_cartao");

            migrationBuilder.DropTable(
                name: "parcela_cartao");

            migrationBuilder.DropTable(
                name: "compra_cartao");

            migrationBuilder.DropTable(
                name: "fatura");

            migrationBuilder.DropTable(
                name: "cartao");

            migrationBuilder.DropColumn(
                name: "Origem",
                table: "transacao");
        }
    }
}
