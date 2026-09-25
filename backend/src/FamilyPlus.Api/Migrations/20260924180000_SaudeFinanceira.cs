using System;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FamilyPlus.Api.Migrations;

[DbContext(typeof(FinanceDbContext))]
[Migration("20260924180000_SaudeFinanceira")]
public partial class SaudeFinanceira : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.10");
        modelBuilder.Entity<PerfilSaudeFinanceira>(entity =>
        {
            entity.ToTable("perfil_saude_financeira");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd().HasColumnType("TEXT");
            entity.Property(x => x.FamiliaId).HasColumnType("TEXT");
            entity.Property(x => x.Ativo).HasColumnType("INTEGER");
            entity.Property(x => x.CriadoEm).HasColumnType("TEXT");
            entity.Property(x => x.AtualizadoEm).HasColumnType("TEXT");
            entity.Property(x => x.MembroId).HasColumnType("TEXT");
            entity.Property(x => x.MetaReservaMeses).HasPrecision(6, 2).HasColumnType("TEXT");
            entity.Property(x => x.TetoComprometimentoPercentual).HasPrecision(6, 2).HasColumnType("TEXT");
            entity.Property(x => x.MetaPoupancaPercentual).HasPrecision(6, 2).HasColumnType("TEXT");
            entity.Property(x => x.Observacao).HasMaxLength(1000).HasColumnType("TEXT");
            entity.Property(x => x.CategoriasEssenciaisJson).IsRequired().HasMaxLength(4000).HasColumnType("TEXT");
            entity.HasIndex(x => x.MembroId);
            entity.HasIndex(x => x.FamiliaId).IsUnique().HasFilter("\"MembroId\" IS NULL");
            entity.HasIndex(x => new { x.FamiliaId, x.MembroId }).IsUnique().HasFilter("\"MembroId\" IS NOT NULL");
        });
    }

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "perfil_saude_financeira",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                FamiliaId = table.Column<Guid>(type: "TEXT", nullable: true),
                Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                CriadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                AtualizadoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                MembroId = table.Column<Guid>(type: "TEXT", nullable: true),
                MetaReservaMeses = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                TetoComprometimentoPercentual = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
                MetaPoupancaPercentual = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: true),
                Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CategoriasEssenciaisJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_perfil_saude_financeira", x => x.Id);
                table.ForeignKey("FK_perfil_saude_financeira_membro_MembroId", x => x.MembroId, "membro", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_perfil_saude_financeira_FamiliaId", table: "perfil_saude_financeira", column: "FamiliaId", unique: true, filter: "\"MembroId\" IS NULL");
        migrationBuilder.CreateIndex(name: "IX_perfil_saude_financeira_FamiliaId_MembroId", table: "perfil_saude_financeira", columns: new[] { "FamiliaId", "MembroId" }, unique: true, filter: "\"MembroId\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_perfil_saude_financeira_MembroId", table: "perfil_saude_financeira", column: "MembroId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "perfil_saude_financeira");
}
