using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint8CoreLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LancamentoId",
                table: "transacao",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LancamentoId",
                table: "rateio_despesa",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LancamentoId",
                table: "ocorrencia_recorrencia",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LancamentoPagamentoId",
                table: "fatura",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataSaldoInicial",
                table: "conta",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Instituicao",
                table: "conta",
                type: "TEXT",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Moeda",
                table: "conta",
                type: "TEXT",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "lancamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MembroId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Natureza = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    OrigemTipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    OrigemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ValorCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    ValorPrevistoCentavos = table.Column<long>(type: "INTEGER", nullable: true),
                    DataCompetencia = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataVencimento = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DataEfetivacao = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lancamento_categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_conta_ContaId",
                        column: x => x.ContaId,
                        principalTable: "conta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_membro_MembroId",
                        column: x => x.MembroId,
                        principalTable: "membro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lancamento_usuario_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Normaliza os fatos existentes para a fonte canônica sem recadastro.
            migrationBuilder.Sql("UPDATE conta SET Moeda = 'BRL' WHERE Moeda = '';");
            migrationBuilder.Sql("UPDATE conta SET DataSaldoInicial = CriadoEm WHERE DataSaldoInicial LIKE '0001-01-01%';");
            migrationBuilder.Sql(@"
                INSERT INTO lancamento
                    (Id, MembroId, ContaId, CategoriaId, Tipo, Natureza, Status, OrigemTipo, OrigemId,
                     Descricao, ValorCentavos, ValorPrevistoCentavos, DataCompetencia, DataVencimento,
                     DataEfetivacao, CriadoPorUsuarioId, Observacao, FamiliaId, Ativo, CriadoEm, AtualizadoEm)
                SELECT Id, MembroId, ContaId, CategoriaId,
                    CASE WHEN Tipo = 0 THEN 'RECEITA' WHEN Tipo = 1 AND Origem = 1 THEN 'PAGAMENTO_FATURA'
                         WHEN Tipo = 1 THEN 'DESPESA' WHEN Tipo = 2 THEN 'TRANSFERENCIA_ENTRADA' ELSE 'TRANSFERENCIA_SAIDA' END,
                    CASE WHEN Tipo = 0 THEN 'RENDA' WHEN Tipo = 1 AND Origem = 1 THEN 'PAGAMENTO_FATURA'
                         WHEN Tipo = 1 THEN 'CONSUMO' ELSE 'TRANSFERENCIA' END,
                    CASE WHEN Status = 0 THEN 'PREVISTO' WHEN Status = 1 THEN 'EFETIVADO' ELSE 'CANCELADO' END,
                    CASE WHEN Tipo IN (2, 3) THEN 'TRANSFERENCIA' WHEN Tipo = 1 AND Origem = 1 THEN 'FATURA' ELSE 'MANUAL' END,
                    CASE WHEN Tipo IN (2, 3) THEN TransferenciaId ELSE NULL END,
                    Descricao, ValorCentavos, CASE WHEN Status = 0 THEN ValorCentavos ELSE NULL END,
                    DataCompetencia, NULL, CASE WHEN Status = 1 THEN DataMovimentacao ELSE NULL END,
                    NULL, Observacao, FamiliaId, Ativo, CriadoEm, AtualizadoEm
                FROM transacao;");
            migrationBuilder.Sql("UPDATE transacao SET LancamentoId = Id;");
            migrationBuilder.Sql(@"
                INSERT INTO lancamento
                    (Id, MembroId, ContaId, CategoriaId, Tipo, Natureza, Status, OrigemTipo, OrigemId,
                     Descricao, ValorCentavos, ValorPrevistoCentavos, DataCompetencia, DataVencimento,
                     DataEfetivacao, CriadoPorUsuarioId, Observacao, FamiliaId, Ativo, CriadoEm, AtualizadoEm)
                SELECT c.Id, c.MembroId, ca.ContaPagamentoPadraoId, c.CategoriaId, 'DESPESA', 'CONSUMO',
                    CASE WHEN c.Status IN (1, 2) THEN 'CANCELADO' ELSE 'EFETIVADO' END, 'COMPRA_CARTAO', c.Id,
                    c.Descricao, c.ValorTotalCentavos, NULL, c.DataCompra, NULL,
                    CASE WHEN c.Status IN (1, 2) THEN NULL ELSE c.DataCompra END, NULL, c.Observacao,
                    c.FamiliaId, c.Ativo, c.CriadoEm, c.AtualizadoEm
                FROM compra_cartao c JOIN cartao ca ON ca.Id = c.CartaoId
                WHERE NOT EXISTS (SELECT 1 FROM lancamento l WHERE l.Id = c.Id);");
            migrationBuilder.Sql(@"
                INSERT INTO lancamento
                    (Id, MembroId, ContaId, CategoriaId, Tipo, Natureza, Status, OrigemTipo, OrigemId,
                     Descricao, ValorCentavos, ValorPrevistoCentavos, DataCompetencia, DataVencimento,
                     DataEfetivacao, CriadoPorUsuarioId, Observacao, FamiliaId, Ativo, CriadoEm, AtualizadoEm)
                SELECT e.Id, c.MembroId, ca.ContaPagamentoPadraoId, c.CategoriaId, 'AJUSTE', 'CONSUMO',
                    'EFETIVADO', 'AJUSTE', e.Id, e.Descricao, -e.ValorCentavos, NULL, e.Data, NULL,
                    e.Data, NULL, e.Descricao, e.FamiliaId, e.Ativo, e.CriadoEm, e.AtualizadoEm
                FROM estorno_cartao e JOIN compra_cartao c ON c.Id = e.CompraCartaoId
                    JOIN cartao ca ON ca.Id = c.CartaoId
                WHERE NOT EXISTS (SELECT 1 FROM lancamento l WHERE l.Id = e.Id);");
            migrationBuilder.Sql(@"
                INSERT INTO lancamento
                    (Id, MembroId, ContaId, CategoriaId, Tipo, Natureza, Status, OrigemTipo, OrigemId,
                     Descricao, ValorCentavos, ValorPrevistoCentavos, DataCompetencia, DataVencimento,
                     DataEfetivacao, CriadoPorUsuarioId, Observacao, FamiliaId, Ativo, CriadoEm, AtualizadoEm)
                SELECT o.Id, r.MembroId, r.ContaId, r.CategoriaId,
                    CASE WHEN r.Tipo = 0 THEN 'RECEITA' ELSE 'DESPESA' END,
                    CASE WHEN r.Tipo = 0 THEN 'RENDA' ELSE 'CONSUMO' END,
                    CASE WHEN o.Status IN (2, 4) THEN 'CANCELADO' ELSE 'PREVISTO' END,
                    'RECORRENCIA', o.Id, r.Descricao,
                    COALESCE(o.ValorRealizadoCentavos, o.ValorPrevistoCentavos), o.ValorPrevistoCentavos,
                    o.DataPrevista, o.DataPrevista, NULL, NULL, o.Observacao, o.FamiliaId, o.Ativo,
                    o.CriadoEm, o.AtualizadoEm
                FROM ocorrencia_recorrencia o JOIN recorrencia r ON r.Id = o.RecorrenciaId
                WHERE o.TransacaoId IS NULL AND NOT EXISTS (SELECT 1 FROM lancamento l WHERE l.Id = o.Id);");
            migrationBuilder.Sql("UPDATE ocorrencia_recorrencia SET LancamentoId = COALESCE(TransacaoId, Id);");

            migrationBuilder.CreateIndex(
                name: "IX_transacao_LancamentoId",
                table: "transacao",
                column: "LancamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rateio_despesa_LancamentoId",
                table: "rateio_despesa",
                column: "LancamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ocorrencia_recorrencia_LancamentoId",
                table: "ocorrencia_recorrencia",
                column: "LancamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fatura_LancamentoPagamentoId",
                table: "fatura",
                column: "LancamentoPagamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_COMPETENCIA",
                table: "lancamento",
                column: "DataCompetencia");

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_CONTA",
                table: "lancamento",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_EFETIVACAO",
                table: "lancamento",
                column: "DataEfetivacao");

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_MEMBRO",
                table: "lancamento",
                column: "MembroId");

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_ORIGEM",
                table: "lancamento",
                columns: new[] { "OrigemTipo", "OrigemId" });

            migrationBuilder.CreateIndex(
                name: "IDX_LANCAMENTO_STATUS",
                table: "lancamento",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_CategoriaId",
                table: "lancamento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_lancamento_CriadoPorUsuarioId",
                table: "lancamento",
                column: "CriadoPorUsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_fatura_lancamento_LancamentoPagamentoId",
                table: "fatura",
                column: "LancamentoPagamentoId",
                principalTable: "lancamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ocorrencia_recorrencia_lancamento_LancamentoId",
                table: "ocorrencia_recorrencia",
                column: "LancamentoId",
                principalTable: "lancamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_rateio_despesa_lancamento_LancamentoId",
                table: "rateio_despesa",
                column: "LancamentoId",
                principalTable: "lancamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_transacao_lancamento_LancamentoId",
                table: "transacao",
                column: "LancamentoId",
                principalTable: "lancamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_fatura_lancamento_LancamentoPagamentoId",
                table: "fatura");

            migrationBuilder.DropForeignKey(
                name: "FK_ocorrencia_recorrencia_lancamento_LancamentoId",
                table: "ocorrencia_recorrencia");

            migrationBuilder.DropForeignKey(
                name: "FK_rateio_despesa_lancamento_LancamentoId",
                table: "rateio_despesa");

            migrationBuilder.DropForeignKey(
                name: "FK_transacao_lancamento_LancamentoId",
                table: "transacao");

            migrationBuilder.DropTable(
                name: "lancamento");

            migrationBuilder.DropIndex(
                name: "IX_transacao_LancamentoId",
                table: "transacao");

            migrationBuilder.DropIndex(
                name: "IX_rateio_despesa_LancamentoId",
                table: "rateio_despesa");

            migrationBuilder.DropIndex(
                name: "IX_ocorrencia_recorrencia_LancamentoId",
                table: "ocorrencia_recorrencia");

            migrationBuilder.DropIndex(
                name: "IX_fatura_LancamentoPagamentoId",
                table: "fatura");

            migrationBuilder.DropColumn(
                name: "LancamentoId",
                table: "transacao");

            migrationBuilder.DropColumn(
                name: "LancamentoId",
                table: "rateio_despesa");

            migrationBuilder.DropColumn(
                name: "LancamentoId",
                table: "ocorrencia_recorrencia");

            migrationBuilder.DropColumn(
                name: "LancamentoPagamentoId",
                table: "fatura");

            migrationBuilder.DropColumn(
                name: "DataSaldoInicial",
                table: "conta");

            migrationBuilder.DropColumn(
                name: "Instituicao",
                table: "conta");

            migrationBuilder.DropColumn(
                name: "Moeda",
                table: "conta");
        }
    }
}
