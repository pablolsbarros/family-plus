using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint6Planejamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ano = table.Column<int>(type: "INTEGER", nullable: false),
                    Mes = table.Column<int>(type: "INTEGER", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    ReceitaPrevistaCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    ModoReceitaPrevista = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orcamento_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_orcamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OrcamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ValorPlanejadoCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_orcamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_orcamento_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_orcamento_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_item_orcamento_orcamento_OrcamentoId",
                        column: x => x.OrcamentoId,
                        principalTable: "orcamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_ITEM_ORCAMENTO_CATEGORIA",
                table: "item_orcamento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IDX_ITEM_ORCAMENTO_MEMBRO",
                table: "item_orcamento",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_ITEM_ORCAMENTO_ORCAMENTO",
                table: "item_orcamento",
                column: "OrcamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_item_orcamento_OrcamentoId_CategoriaId_MembroId",
                table: "item_orcamento",
                columns: new[] { "OrcamentoId", "CategoriaId", "MembroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_ORCAMENTO_ANO_MES",
                table: "orcamento",
                columns: new[] { "Ano", "Mes" });

            migrationBuilder.CreateIndex(
                name: "IDX_ORCAMENTO_MEMBRO",
                table: "orcamento",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_ORCAMENTO_STATUS",
                table: "orcamento",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_orcamento");

            migrationBuilder.DropTable(
                name: "orcamento");
        }
    }
}
