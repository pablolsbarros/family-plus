using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations
{
    /// <inheritdoc />
    public partial class Sprint5DashboardAlertas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChaveUnica = table.Column<string>(type: "TEXT", maxLength: 240, nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Severidade = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Mensagem = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EntidadeOrigem = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DataGeracao = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DataLeitura = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Resolvido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerta", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_DATA",
                table: "alerta",
                column: "DataGeracao");

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_RESOLVIDO",
                table: "alerta",
                column: "Resolvido");

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_SEVERIDADE",
                table: "alerta",
                column: "Severidade");

            migrationBuilder.CreateIndex(
                name: "IDX_ALERTA_TIPO",
                table: "alerta",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_alerta_ChaveUnica_Resolvido",
                table: "alerta",
                columns: new[] { "ChaveUnica", "Resolvido" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerta");
        }
    }
}
