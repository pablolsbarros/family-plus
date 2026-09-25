using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FamilyPlus.Api.Data;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public Guid? CurrentFamiliaId { get; private set; }
    public void SetFamilyContext(Guid? familiaId) => CurrentFamiliaId = familiaId;

    public DbSet<Familia> Familias => Set<Familia>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SessaoLocal> SessoesLocais => Set<SessaoLocal>();
    public DbSet<Membro> Membros => Set<Membro>();
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Transacao> Transacoes => Set<Transacao>();
    public DbSet<Transferencia> Transferencias => Set<Transferencia>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();
    public DbSet<Cartao> Cartoes => Set<Cartao>();
    public DbSet<CompraCartao> ComprasCartao => Set<CompraCartao>();
    public DbSet<ParcelaCartao> ParcelasCartao => Set<ParcelaCartao>();
    public DbSet<Fatura> Faturas => Set<Fatura>();
    public DbSet<EstornoCartao> EstornosCartao => Set<EstornoCartao>();
    public DbSet<Recorrencia> Recorrencias => Set<Recorrencia>();
    public DbSet<OcorrenciaRecorrencia> OcorrenciasRecorrentes => Set<OcorrenciaRecorrencia>();
    public DbSet<Assinatura> Assinaturas => Set<Assinatura>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<ItemOrcamento> ItensOrcamento => Set<ItemOrcamento>();
    public DbSet<PerfilAcesso> PerfisAcesso => Set<PerfilAcesso>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<PerfilPermissao> PerfisPermissoes => Set<PerfilPermissao>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
    public DbSet<PermissaoEscopo> PermissoesEscopo => Set<PermissaoEscopo>();
    public DbSet<PermissaoMembro> PermissoesMembro => Set<PermissaoMembro>();
    public DbSet<ConfiguracaoFamilia> ConfiguracoesFamilia => Set<ConfiguracaoFamilia>();
    public DbSet<PreferenciaUsuario> PreferenciasUsuarios => Set<PreferenciaUsuario>();
    public DbSet<ConfiguracaoNotificacao> ConfiguracoesNotificacao => Set<ConfiguracaoNotificacao>();
    public DbSet<RateioDespesa> RateiosDespesa => Set<RateioDespesa>();
    public DbSet<RateioDespesaItem> RateiosDespesaItens => Set<RateioDespesaItem>();
    public DbSet<ConexaoFinanceira> ConexoesFinanceiras => Set<ConexaoFinanceira>();
    public DbSet<Importacao> Importacoes => Set<Importacao>();
    public DbSet<ImportacaoArquivo> ImportacoesArquivos => Set<ImportacaoArquivo>();
    public DbSet<ImportacaoItem> ImportacoesItens => Set<ImportacaoItem>();
    public DbSet<PerfilSaudeFinanceira> PerfisSaudeFinanceira => Set<PerfilSaudeFinanceira>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuario");
            entity.HasIndex(x => x.Login).IsUnique();
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Login).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SenhaHash).HasMaxLength(512).IsRequired();
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<SessaoLocal>(entity =>
        {
            entity.ToTable("sessao_local");
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.Usuario).WithMany(x => x.Sessoes).HasForeignKey(x => x.UsuarioId);
        });
        modelBuilder.Entity<Membro>(entity =>
        {
            entity.ToTable("membro");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Descricao).HasMaxLength(500);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(x => x.Familia).WithMany(x => x.Membros).HasForeignKey(x => x.FamiliaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Familia>(entity =>
        {
            entity.ToTable("familia");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.MoedaPadrao).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Timezone).HasMaxLength(80).IsRequired();
        });
        modelBuilder.Entity<Conta>(entity =>
        {
            entity.ToTable("conta");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(500);
            entity.Property(x => x.Instituicao).HasMaxLength(160);
            entity.Property(x => x.Moeda).HasMaxLength(3).IsRequired();
            entity.HasOne(x => x.Membro).WithMany(x => x.Contas).HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categoria");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.HasOne(x => x.CategoriaPai).WithMany(x => x.Filhas).HasForeignKey(x => x.CategoriaPaiId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Lancamento>(entity =>
        {
            entity.ToTable("lancamento");
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Natureza).HasConversion<string>().HasMaxLength(30);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.OrigemTipo).HasConversion<string>().HasMaxLength(30);
            entity.HasIndex(x => new { x.OrigemTipo, x.OrigemId }).HasDatabaseName("IDX_LANCAMENTO_ORIGEM");
            entity.HasIndex(x => x.DataCompetencia).HasDatabaseName("IDX_LANCAMENTO_COMPETENCIA");
            entity.HasIndex(x => x.DataEfetivacao).HasDatabaseName("IDX_LANCAMENTO_EFETIVACAO");
            entity.HasIndex(x => x.ContaId).HasDatabaseName("IDX_LANCAMENTO_CONTA");
            entity.HasIndex(x => x.MembroId).HasDatabaseName("IDX_LANCAMENTO_MEMBRO");
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_LANCAMENTO_STATUS");
            entity.HasOne(x => x.Conta).WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CriadoPorUsuarioId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Transferencia>(entity =>
        {
            entity.ToTable("transferencia");
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Data);
            entity.HasIndex(x => x.ContaOrigemId);
            entity.HasIndex(x => x.ContaDestinoId);
            entity.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaOrigemId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaDestinoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Membro>().WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Transacao>(entity =>
        {
            entity.ToTable("transacao");
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.Origem).HasDefaultValue(OrigemTransacao.NORMAL);
            entity.HasIndex(x => x.DataMovimentacao).HasDatabaseName("IDX_TRANSACAO_DATA");
            entity.HasIndex(x => x.ContaId).HasDatabaseName("IDX_TRANSACAO_CONTA");
            entity.HasIndex(x => x.CategoriaId).HasDatabaseName("IDX_TRANSACAO_CATEGORIA");
            entity.HasIndex(x => x.MembroId).HasDatabaseName("IDX_TRANSACAO_MEMBRO");
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_TRANSACAO_STATUS");
            entity.HasIndex(x => x.TransferenciaId);
            entity.HasIndex(x => x.LancamentoId).IsUnique();
            entity.HasOne(x => x.Conta).WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Transferencia).WithMany(x => x.Transacoes).HasForeignKey(x => x.TransferenciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Lancamento>().WithMany().HasForeignKey(x => x.LancamentoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.ToTable("auditoria");
            entity.Property(x => x.Entidade).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => new { x.Entidade, x.EntidadeId });
            entity.HasIndex(x => x.Data);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Configuracao>(entity =>
        {
            entity.ToTable("configuracao");
            entity.HasIndex(x => x.Chave).IsUnique();
            entity.Property(x => x.Chave).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Valor).HasMaxLength(2000).IsRequired();
        });
        modelBuilder.Entity<PerfilSaudeFinanceira>(entity =>
        {
            entity.ToTable("perfil_saude_financeira");
            entity.Property(x => x.MetaReservaMeses).HasPrecision(6, 2);
            entity.Property(x => x.TetoComprometimentoPercentual).HasPrecision(6, 2);
            entity.Property(x => x.MetaPoupancaPercentual).HasPrecision(6, 2);
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.Property(x => x.CategoriasEssenciaisJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => x.FamiliaId).IsUnique().HasFilter("\"MembroId\" IS NULL");
            entity.HasIndex(x => new { x.FamiliaId, x.MembroId }).IsUnique().HasFilter("\"MembroId\" IS NOT NULL");
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Cartao>(entity =>
        {
            entity.ToTable("cartao");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Bandeira).HasMaxLength(60).IsRequired();
            entity.Property(x => x.UltimosDigitos).HasMaxLength(4).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(500);
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ContaPagamentoPadrao).WithMany().HasForeignKey(x => x.ContaPagamentoPadraoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Fatura>(entity =>
        {
            entity.ToTable("fatura");
            entity.HasIndex(x => x.CartaoId).HasDatabaseName("IDX_FATURA_CARTAO");
            entity.HasIndex(x => x.Competencia).HasDatabaseName("IDX_FATURA_COMPETENCIA");
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_FATURA_STATUS");
            entity.HasIndex(x => new { x.CartaoId, x.Competencia }).IsUnique();
            entity.HasIndex(x => x.LancamentoPagamentoId).IsUnique();
            entity.HasOne(x => x.Cartao).WithMany(x => x.Faturas).HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ContaPagamento).WithMany().HasForeignKey(x => x.ContaPagamentoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Lancamento>().WithMany().HasForeignKey(x => x.LancamentoPagamentoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CompraCartao>(entity =>
        {
            entity.ToTable("compra_cartao");
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => x.CartaoId).HasDatabaseName("IDX_COMPRA_CARTAO");
            entity.HasIndex(x => x.DataCompra).HasDatabaseName("IDX_COMPRA_DATA");
            entity.HasOne(x => x.Cartao).WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ParcelaCartao>(entity =>
        {
            entity.ToTable("parcela_cartao");
            entity.HasIndex(x => x.FaturaId).HasDatabaseName("IDX_PARCELA_FATURA");
            entity.HasIndex(x => x.CompraCartaoId).HasDatabaseName("IDX_PARCELA_COMPRA");
            entity.HasIndex(x => x.DataCompetencia).HasDatabaseName("IDX_PARCELA_COMPETENCIA");
            entity.HasOne(x => x.CompraCartao).WithMany(x => x.Parcelas).HasForeignKey(x => x.CompraCartaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Fatura).WithMany(x => x.Parcelas).HasForeignKey(x => x.FaturaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EstornoCartao>(entity =>
        {
            entity.ToTable("estorno_cartao");
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            entity.HasOne(x => x.CompraCartao).WithMany(x => x.Estornos).HasForeignKey(x => x.CompraCartaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Fatura).WithMany(x => x.Estornos).HasForeignKey(x => x.FaturaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Recorrencia>(entity =>
        {
            entity.ToTable("recorrencia");
            entity.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_RECORRENCIA_STATUS");
            entity.HasIndex(x => x.ProximaOcorrencia).HasDatabaseName("IDX_RECORRENCIA_PROXIMA");
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Conta).WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OcorrenciaRecorrencia>(entity =>
        {
            entity.ToTable("ocorrencia_recorrencia");
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => x.DataPrevista).HasDatabaseName("IDX_OCORRENCIA_DATA");
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_OCORRENCIA_STATUS");
            entity.HasIndex(x => x.RecorrenciaId).HasDatabaseName("IDX_OCORRENCIA_RECORRENCIA");
            entity.HasIndex(x => new { x.RecorrenciaId, x.DataPrevista }).IsUnique();
            entity.HasIndex(x => x.LancamentoId).IsUnique();
            entity.HasOne(x => x.Recorrencia).WithMany(x => x.Ocorrencias).HasForeignKey(x => x.RecorrenciaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Transacao).WithMany().HasForeignKey(x => x.TransacaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CompraCartao).WithMany().HasForeignKey(x => x.CompraCartaoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Lancamento>().WithMany().HasForeignKey(x => x.LancamentoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Assinatura>(entity =>
        {
            entity.ToTable("assinatura");
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => x.Ativo).HasDatabaseName("IDX_ASSINATURA_ATIVA");
            entity.HasIndex(x => x.ProximaCobranca).HasDatabaseName("IDX_ASSINATURA_PROXIMA_COBRANCA");
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Conta).WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Cartao).WithMany().HasForeignKey(x => x.CartaoId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Alerta>(entity =>
        {
            entity.ToTable("alerta");
            entity.Property(x => x.ChaveUnica).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Titulo).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Mensagem).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.EntidadeOrigem).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.DataGeracao).HasDatabaseName("IDX_ALERTA_DATA");
            entity.HasIndex(x => x.Tipo).HasDatabaseName("IDX_ALERTA_TIPO");
            entity.HasIndex(x => x.Severidade).HasDatabaseName("IDX_ALERTA_SEVERIDADE");
            entity.HasIndex(x => x.Resolvido).HasDatabaseName("IDX_ALERTA_RESOLVIDO");
            entity.HasIndex(x => new { x.ChaveUnica, x.Resolvido });
        });
        modelBuilder.Entity<Orcamento>(entity =>
        {
            entity.ToTable("orcamento");
            entity.Property(x => x.Descricao).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => new { x.Ano, x.Mes }).HasDatabaseName("IDX_ORCAMENTO_ANO_MES");
            entity.HasIndex(x => x.MembroId).HasDatabaseName("IDX_ORCAMENTO_MEMBRO");
            entity.HasIndex(x => x.Status).HasDatabaseName("IDX_ORCAMENTO_STATUS");
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ItemOrcamento>(entity =>
        {
            entity.ToTable("item_orcamento");
            entity.Property(x => x.Observacao).HasMaxLength(1000);
            entity.HasIndex(x => x.OrcamentoId).HasDatabaseName("IDX_ITEM_ORCAMENTO_ORCAMENTO");
            entity.HasIndex(x => x.CategoriaId).HasDatabaseName("IDX_ITEM_ORCAMENTO_CATEGORIA");
            entity.HasIndex(x => x.MembroId).HasDatabaseName("IDX_ITEM_ORCAMENTO_MEMBRO");
            entity.HasIndex(x => new { x.OrcamentoId, x.CategoriaId, x.MembroId }).IsUnique();
            entity.HasOne(x => x.Orcamento).WithMany(x => x.Itens).HasForeignKey(x => x.OrcamentoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PerfilAcesso>(entity => { entity.ToTable("perfil_acesso"); entity.Property(x => x.Nome).HasMaxLength(60).IsRequired(); entity.HasIndex(x => new { x.FamiliaId, x.Nome }).IsUnique(); });
        modelBuilder.Entity<Permissao>(entity => { entity.ToTable("permissao"); entity.Property(x => x.Codigo).HasMaxLength(100).IsRequired(); entity.Property(x => x.Modulo).HasMaxLength(80).IsRequired(); entity.Property(x => x.Descricao).HasMaxLength(240).IsRequired(); entity.HasIndex(x => new { x.FamiliaId, x.Codigo }).IsUnique(); });
        modelBuilder.Entity<PerfilPermissao>(entity => { entity.ToTable("perfil_permissao"); entity.HasKey(x => new { x.PerfilAcessoId, x.PermissaoId }); entity.HasOne(x => x.PerfilAcesso).WithMany(x => x.Permissoes).HasForeignKey(x => x.PerfilAcessoId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Permissao).WithMany(x => x.Perfis).HasForeignKey(x => x.PermissaoId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<UsuarioPerfil>(entity => { entity.ToTable("usuario_perfil"); entity.HasKey(x => new { x.UsuarioId, x.PerfilAcessoId }); entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.PerfilAcesso).WithMany(x => x.Usuarios).HasForeignKey(x => x.PerfilAcessoId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PermissaoEscopo>(entity => { entity.ToTable("permissao_escopo"); entity.Property(x => x.Recurso).HasMaxLength(80).IsRequired(); entity.Property(x => x.TipoEscopo).HasConversion<string>().HasMaxLength(20); entity.HasIndex(x => new { x.UsuarioId, x.Recurso }).IsUnique(); entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PermissaoMembro>(entity => { entity.ToTable("permissao_membro"); entity.HasKey(x => new { x.PermissaoEscopoId, x.MembroId }); entity.HasOne(x => x.PermissaoEscopo).WithMany(x => x.Membros).HasForeignKey(x => x.PermissaoEscopoId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ConfiguracaoFamilia>(entity => { entity.ToTable("configuracao_familia"); entity.HasKey(x => x.FamiliaId); entity.Property(x => x.Moeda).HasMaxLength(3).IsRequired(); entity.Property(x => x.Timezone).HasMaxLength(80).IsRequired(); entity.Property(x => x.Locale).HasMaxLength(20).IsRequired(); entity.HasOne(x => x.Familia).WithOne().HasForeignKey<ConfiguracaoFamilia>(x => x.FamiliaId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<PreferenciaUsuario>(entity => { entity.ToTable("preferencia_usuario"); entity.HasKey(x => x.UsuarioId); entity.Property(x => x.Idioma).HasMaxLength(20).IsRequired(); entity.Property(x => x.FormatoData).HasMaxLength(30).IsRequired(); entity.Property(x => x.PaginaInicial).HasMaxLength(80).IsRequired(); entity.Property(x => x.Tema).HasMaxLength(20).IsRequired(); entity.HasOne(x => x.Usuario).WithOne().HasForeignKey<PreferenciaUsuario>(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ConfiguracaoNotificacao>(entity => { entity.ToTable("configuracao_notificacao"); entity.HasKey(x => x.UsuarioId); entity.HasOne(x => x.Usuario).WithOne().HasForeignKey<ConfiguracaoNotificacao>(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RateioDespesa>(entity => { entity.ToTable("rateio_despesa"); entity.HasIndex(x => x.TransacaoId).IsUnique(); entity.HasIndex(x => x.LancamentoId).IsUnique(); entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20); entity.HasOne(x => x.Transacao).WithMany().HasForeignKey(x => x.TransacaoId).OnDelete(DeleteBehavior.Cascade); entity.HasOne<Lancamento>().WithMany().HasForeignKey(x => x.LancamentoId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<RateioDespesaItem>(entity => { entity.ToTable("rateio_despesa_item"); entity.Property(x => x.Percentual).HasPrecision(12, 4); entity.HasOne(x => x.RateioDespesa).WithMany(x => x.Itens).HasForeignKey(x => x.RateioDespesaId).OnDelete(DeleteBehavior.Cascade); entity.HasOne(x => x.Membro).WithMany().HasForeignKey(x => x.MembroId).OnDelete(DeleteBehavior.Restrict); });
        modelBuilder.Entity<ConexaoFinanceira>(entity => { entity.ToTable("conexao_financeira"); entity.Property(x => x.Instituicao).HasMaxLength(160).IsRequired(); entity.Property(x => x.Configuracao).HasMaxLength(4000); entity.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); });
        modelBuilder.Entity<Importacao>(entity => { entity.ToTable("importacao"); entity.Property(x => x.NomeArquivo).HasMaxLength(240).IsRequired(); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); });
        modelBuilder.Entity<ImportacaoArquivo>(entity => { entity.ToTable("importacao_arquivo"); entity.Property(x => x.Nome).HasMaxLength(240).IsRequired(); entity.Property(x => x.Tipo).HasMaxLength(20).IsRequired(); entity.HasOne(x => x.Importacao).WithMany(x => x.Arquivos).HasForeignKey(x => x.ImportacaoId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ImportacaoItem>(entity => { entity.ToTable("importacao_item"); entity.Property(x => x.Dados).HasMaxLength(4000).IsRequired(); entity.Property(x => x.Erro).HasMaxLength(1000); entity.HasOne(x => x.Importacao).WithMany(x => x.Itens).HasForeignKey(x => x.ImportacaoId).OnDelete(DeleteBehavior.Cascade); });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(x => typeof(EntityBase).IsAssignableFrom(x.ClrType) && x.ClrType != typeof(Familia)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var familyProperty = Expression.Property(parameter, nameof(EntityBase.FamiliaId));
            var currentFamily = Expression.Property(Expression.Constant(this), nameof(CurrentFamiliaId));
            var filter = Expression.OrElse(Expression.Equal(currentFamily, Expression.Constant(null, typeof(Guid?))), Expression.Equal(familyProperty, currentFamily));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(filter, parameter));
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyFamilyAndTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyFamilyAndTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyFamilyAndTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<EntityBase>().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.AtualizadoEm = DateTimeOffset.UtcNow;
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CriadoEm = DateTimeOffset.UtcNow;
                if (CurrentFamiliaId.HasValue && entry.Entity is not Familia) entry.Entity.FamiliaId ??= CurrentFamiliaId;
            }
        }
    }
}
