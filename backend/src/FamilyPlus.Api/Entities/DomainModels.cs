namespace FamilyPlus.Api.Entities;

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FamiliaId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Usuario : EntityBase
{
    public required string Nome { get; set; }
    public required string Login { get; set; }
    public required string SenhaHash { get; set; }
    public Guid? MembroId { get; set; }
    public Membro? Membro { get; set; }
    public DateTimeOffset? UltimoAcessoEm { get; set; }
    public ICollection<SessaoLocal> Sessoes { get; set; } = new List<SessaoLocal>();
}

public sealed class SessaoLocal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset CriadaEm { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiraEm { get; set; }
    public DateTimeOffset? EncerradaEm { get; set; }
    public Usuario? Usuario { get; set; }
}

public sealed class Membro : EntityBase
{
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public TipoMembro Tipo { get; set; } = TipoMembro.TITULAR;
    public DateTimeOffset? DataNascimento { get; set; }
    public Familia? Familia { get; set; }
    public ICollection<Conta> Contas { get; set; } = new List<Conta>();
}

public sealed class Familia : EntityBase
{
    public required string Nome { get; set; }
    public string MoedaPadrao { get; set; } = "BRL";
    public string Timezone { get; set; } = "America/Cuiaba";
    public bool Ativa { get => Ativo; set => Ativo = value; }
    public ICollection<Membro> Membros { get; set; } = new List<Membro>();
}

public enum TipoMembro { TITULAR, CONJUGE, DEPENDENTE, OUTRO }

public enum PerfilAcessoNome { ADMINISTRADOR, MEMBRO, SOMENTE_LEITURA }

public sealed class PerfilAcesso : EntityBase
{
    public required string Nome { get; set; }
    public bool Sistema { get; set; } = true;
    public ICollection<PerfilPermissao> Permissoes { get; set; } = new List<PerfilPermissao>();
    public ICollection<UsuarioPerfil> Usuarios { get; set; } = new List<UsuarioPerfil>();
}

public sealed class Permissao : EntityBase
{
    public required string Codigo { get; set; }
    public required string Modulo { get; set; }
    public required string Descricao { get; set; }
    public ICollection<PerfilPermissao> Perfis { get; set; } = new List<PerfilPermissao>();
}

public sealed class PerfilPermissao
{
    public Guid PerfilAcessoId { get; set; }
    public Guid PermissaoId { get; set; }
    public PerfilAcesso? PerfilAcesso { get; set; }
    public Permissao? Permissao { get; set; }
}

public sealed class UsuarioPerfil
{
    public Guid UsuarioId { get; set; }
    public Guid PerfilAcessoId { get; set; }
    public Usuario? Usuario { get; set; }
    public PerfilAcesso? PerfilAcesso { get; set; }
}

public enum TipoEscopo { PROPRIOS, FAMILIA, SELECIONADOS }

public sealed class PermissaoEscopo : EntityBase
{
    public Guid UsuarioId { get; set; }
    public required string Recurso { get; set; }
    public TipoEscopo TipoEscopo { get; set; }
    public Usuario? Usuario { get; set; }
    public ICollection<PermissaoMembro> Membros { get; set; } = new List<PermissaoMembro>();
}

public sealed class PermissaoMembro
{
    public Guid PermissaoEscopoId { get; set; }
    public Guid MembroId { get; set; }
    public PermissaoEscopo? PermissaoEscopo { get; set; }
    public Membro? Membro { get; set; }
}

public enum TipoConta { ContaCorrente, ContaPoupanca, Carteira, Dinheiro, ContaDigital, Outros }

public sealed class Conta : EntityBase
{
    public Guid MembroId { get; set; }
    public required string Nome { get; set; }
    public TipoConta Tipo { get; set; }
    public string? Instituicao { get; set; }
    public string Moeda { get; set; } = "BRL";
    public long SaldoInicialCentavos { get; set; }
    public DateTimeOffset DataSaldoInicial { get; set; } = DateTimeOffset.UtcNow;
    public string? Observacao { get; set; }
    public Membro? Membro { get; set; }
}

public enum TipoCategoria { Receita, Despesa }

public sealed class Categoria : EntityBase
{
    public required string Nome { get; set; }
    public TipoCategoria Tipo { get; set; }
    public Guid? CategoriaPaiId { get; set; }
    public Categoria? CategoriaPai { get; set; }
    public ICollection<Categoria> Filhas { get; set; } = new List<Categoria>();
}

public enum TipoTransacao { RECEITA, DESPESA, TRANSFERENCIA_ENTRADA, TRANSFERENCIA_SAIDA }
public enum StatusTransacao { PREVISTA, EFETIVADA, CANCELADA }
public enum OrigemTransacao { NORMAL, PAGAMENTO_FATURA }

public enum TipoLancamento { RECEITA, DESPESA, TRANSFERENCIA_ENTRADA, TRANSFERENCIA_SAIDA, PAGAMENTO_FATURA, AJUSTE }
public enum NaturezaLancamento { CONSUMO, RENDA, TRANSFERENCIA, PAGAMENTO_FATURA, AJUSTE }
public enum StatusLancamento { PREVISTO, EFETIVADO, CANCELADO }
public enum OrigemLancamento { MANUAL, TRANSFERENCIA, COMPRA_CARTAO, FATURA, RECORRENCIA, ASSINATURA, IMPORTACAO, AJUSTE }

public sealed class Lancamento : EntityBase
{
    public Guid MembroId { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public TipoLancamento Tipo { get; set; }
    public NaturezaLancamento Natureza { get; set; }
    public StatusLancamento Status { get; set; }
    public OrigemLancamento OrigemTipo { get; set; } = OrigemLancamento.MANUAL;
    public Guid? OrigemId { get; set; }
    public required string Descricao { get; set; }
    public long ValorCentavos { get; set; }
    public long? ValorPrevistoCentavos { get; set; }
    public DateTimeOffset DataCompetencia { get; set; }
    public DateTimeOffset? DataVencimento { get; set; }
    public DateTimeOffset? DataEfetivacao { get; set; }
    public Guid? CriadoPorUsuarioId { get; set; }
    public string? Observacao { get; set; }
    public Conta? Conta { get; set; }
    public Membro? Membro { get; set; }
    public Categoria? Categoria { get; set; }
}

public sealed class Transferencia : EntityBase
{
    public Guid ContaOrigemId { get; set; }
    public Guid ContaDestinoId { get; set; }
    public Guid MembroId { get; set; }
    public long ValorCentavos { get; set; }
    public DateTimeOffset Data { get; set; }
    public required string Descricao { get; set; }
    public StatusTransacao Status { get; set; }
    public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
}

public sealed class Transacao : EntityBase
{
    public Guid MembroId { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public TipoTransacao Tipo { get; set; }
    public required string Descricao { get; set; }
    public long ValorCentavos { get; set; }
    public DateTimeOffset DataCompetencia { get; set; }
    public DateTimeOffset DataMovimentacao { get; set; }
    public StatusTransacao Status { get; set; }
    public OrigemTransacao Origem { get; set; } = OrigemTransacao.NORMAL;
    public string? Observacao { get; set; }
    public Guid? TransferenciaId { get; set; }
    public Guid? LancamentoId { get; set; }
    public Conta? Conta { get; set; }
    public Membro? Membro { get; set; }
    public Categoria? Categoria { get; set; }
    public Transferencia? Transferencia { get; set; }
}

public enum StatusCompraCartao { ATIVA, CANCELADA, ESTORNADA }
public enum StatusFatura { ABERTA, FECHADA, PAGA, VENCIDA, CANCELADA }

public sealed class Cartao : EntityBase
{
    public Guid MembroId { get; set; }
    public required string Nome { get; set; }
    public required string Bandeira { get; set; }
    public required string UltimosDigitos { get; set; }
    public long LimiteTotalCentavos { get; set; }
    public int DiaFechamento { get; set; }
    public int DiaVencimento { get; set; }
    public Guid ContaPagamentoPadraoId { get; set; }
    public string? Observacao { get; set; }
    public Membro? Membro { get; set; }
    public Conta? ContaPagamentoPadrao { get; set; }
    public ICollection<Fatura> Faturas { get; set; } = new List<Fatura>();
}

public sealed class CompraCartao : EntityBase
{
    public Guid CartaoId { get; set; }
    public Guid MembroId { get; set; }
    public Guid CategoriaId { get; set; }
    public required string Descricao { get; set; }
    public long ValorTotalCentavos { get; set; }
    public int QuantidadeParcelas { get; set; }
    public DateTimeOffset DataCompra { get; set; }
    public string? Observacao { get; set; }
    public StatusCompraCartao Status { get; set; } = StatusCompraCartao.ATIVA;
    public Cartao? Cartao { get; set; }
    public Membro? Membro { get; set; }
    public Categoria? Categoria { get; set; }
    public ICollection<ParcelaCartao> Parcelas { get; set; } = new List<ParcelaCartao>();
    public ICollection<EstornoCartao> Estornos { get; set; } = new List<EstornoCartao>();
}

public sealed class Fatura : EntityBase
{
    public Guid CartaoId { get; set; }
    public DateTimeOffset Competencia { get; set; }
    public DateTimeOffset DataFechamento { get; set; }
    public DateTimeOffset DataVencimento { get; set; }
    public long ValorTotalCentavos { get; set; }
    public StatusFatura Status { get; set; } = StatusFatura.ABERTA;
    public DateTimeOffset? DataPagamento { get; set; }
    public Guid? ContaPagamentoId { get; set; }
    public Guid? LancamentoPagamentoId { get; set; }
    public Cartao? Cartao { get; set; }
    public Conta? ContaPagamento { get; set; }
    public ICollection<ParcelaCartao> Parcelas { get; set; } = new List<ParcelaCartao>();
    public ICollection<EstornoCartao> Estornos { get; set; } = new List<EstornoCartao>();
}

public sealed class ParcelaCartao : EntityBase
{
    public Guid CompraCartaoId { get; set; }
    public int NumeroParcela { get; set; }
    public int QuantidadeTotal { get; set; }
    public long ValorCentavos { get; set; }
    public DateTimeOffset DataCompetencia { get; set; }
    public Guid FaturaId { get; set; }
    public StatusCompraCartao Status { get; set; } = StatusCompraCartao.ATIVA;
    public CompraCartao? CompraCartao { get; set; }
    public Fatura? Fatura { get; set; }
}

public sealed class EstornoCartao : EntityBase
{
    public Guid CompraCartaoId { get; set; }
    public Guid FaturaId { get; set; }
    public long ValorCentavos { get; set; }
    public DateTimeOffset Data { get; set; }
    public required string Descricao { get; set; }
    public CompraCartao? CompraCartao { get; set; }
    public Fatura? Fatura { get; set; }
}

public enum FrequenciaRecorrencia { DIARIA, SEMANAL, QUINZENAL, MENSAL, BIMESTRAL, TRIMESTRAL, SEMESTRAL, ANUAL }
public enum StatusRecorrencia { ATIVA, PAUSADA, ENCERRADA }
public enum TipoRecorrencia { NORMAL, CONTA_FIXA }
public enum StatusOcorrenciaRecorrencia { PENDENTE, EFETIVADA, IGNORADA, ATRASADA, CANCELADA }
public enum PeriodicidadeAssinatura { MENSAL, BIMESTRAL, TRIMESTRAL, SEMESTRAL, ANUAL }
public enum MetodoPagamentoAssinatura { CONTA, CARTAO }

public sealed class Recorrencia : EntityBase
{
    public Guid MembroId { get; set; }
    public Guid ContaId { get; set; }
    public Guid CategoriaId { get; set; }
    public TipoTransacao Tipo { get; set; }
    public required string Descricao { get; set; }
    public long ValorCentavos { get; set; }
    public FrequenciaRecorrencia Frequencia { get; set; }
    public DateTimeOffset DataInicio { get; set; }
    public DateTimeOffset? DataFim { get; set; }
    public DateTimeOffset ProximaOcorrencia { get; set; }
    public int DiaReferencia { get; set; }
    public bool GerarAutomaticamente { get; set; } = true;
    public bool ValorVariavel { get; set; }
    public TipoRecorrencia Classificacao { get; set; } = TipoRecorrencia.NORMAL;
    public StatusRecorrencia Status { get; set; } = StatusRecorrencia.ATIVA;
    public string? Observacao { get; set; }
    public Membro? Membro { get; set; }
    public Conta? Conta { get; set; }
    public Categoria? Categoria { get; set; }
    public ICollection<OcorrenciaRecorrencia> Ocorrencias { get; set; } = new List<OcorrenciaRecorrencia>();
}

public sealed class OcorrenciaRecorrencia : EntityBase
{
    public Guid RecorrenciaId { get; set; }
    public DateTimeOffset DataPrevista { get; set; }
    public long ValorPrevistoCentavos { get; set; }
    public long? ValorRealizadoCentavos { get; set; }
    public StatusOcorrenciaRecorrencia Status { get; set; } = StatusOcorrenciaRecorrencia.PENDENTE;
    public Guid? TransacaoId { get; set; }
    public Guid? LancamentoId { get; set; }
    public Guid? CompraCartaoId { get; set; }
    public string? Observacao { get; set; }
    public Recorrencia? Recorrencia { get; set; }
    public Transacao? Transacao { get; set; }
    public CompraCartao? CompraCartao { get; set; }
}

public sealed class Assinatura : EntityBase
{
    public Guid MembroId { get; set; }
    public required string Nome { get; set; }
    public Guid CategoriaId { get; set; }
    public Guid ContaId { get; set; }
    public long ValorCentavos { get; set; }
    public PeriodicidadeAssinatura Periodicidade { get; set; }
    public DateTimeOffset DataInicio { get; set; }
    public DateTimeOffset ProximaCobranca { get; set; }
    public MetodoPagamentoAssinatura MetodoPagamento { get; set; }
    public Guid? CartaoId { get; set; }
    public DateTimeOffset? DataCancelamento { get; set; }
    public string? Observacao { get; set; }
    public Membro? Membro { get; set; }
    public Categoria? Categoria { get; set; }
    public Conta? Conta { get; set; }
    public Cartao? Cartao { get; set; }
}

public enum StatusOrcamento { RASCUNHO, ATIVO, ENCERRADO }
public enum ModoReceitaPrevista { MANUAL, RECORRENCIAS }

public sealed class Orcamento : EntityBase
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public Guid? MembroId { get; set; }
    public required string Descricao { get; set; }
    public long ReceitaPrevistaCentavos { get; set; }
    public ModoReceitaPrevista ModoReceitaPrevista { get; set; } = ModoReceitaPrevista.MANUAL;
    public StatusOrcamento Status { get; set; } = StatusOrcamento.RASCUNHO;
    public string? Observacao { get; set; }
    public Membro? Membro { get; set; }
    public ICollection<ItemOrcamento> Itens { get; set; } = new List<ItemOrcamento>();
}

public sealed class ItemOrcamento : EntityBase
{
    public Guid OrcamentoId { get; set; }
    public Guid CategoriaId { get; set; }
    public Guid? MembroId { get; set; }
    public long ValorPlanejadoCentavos { get; set; }
    public string? Observacao { get; set; }
    public Orcamento? Orcamento { get; set; }
    public Categoria? Categoria { get; set; }
    public Membro? Membro { get; set; }
}

public enum TipoAlerta { CONTA_VENCIDA, CONTA_PROXIMA, FATURA_PROXIMA, FATURA_VENCIDA, SALDO_NEGATIVO, SALDO_PROJETADO_NEGATIVO, LIMITE_CARTAO_ALTO, RECORRENCIA_ATRASADA, ORCAMENTO_ATENCAO, ORCAMENTO_CRITICO, ORCAMENTO_ESTOURADO, ORCAMENTO_ESTOURO_PROJETADO, CATEGORIA_SEM_ORCAMENTO }
public enum SeveridadeAlerta { INFORMACAO, ATENCAO, CRITICO }

public sealed class Alerta : EntityBase
{
    public required string ChaveUnica { get; set; }
    public TipoAlerta Tipo { get; set; }
    public SeveridadeAlerta Severidade { get; set; }
    public required string Titulo { get; set; }
    public required string Mensagem { get; set; }
    public required string EntidadeOrigem { get; set; }
    public Guid? EntidadeId { get; set; }
    public DateTimeOffset DataGeracao { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DataLeitura { get; set; }
    public bool Resolvido { get; set; }
}

public enum OperacaoAuditoria { CRIACAO, ALTERACAO, CANCELAMENTO, EFETIVACAO, ESTORNO, PAGAMENTO, AJUSTE, LOGIN, LOGOUT, ALTERACAO_PERMISSAO, ALTERACAO_MEMBRO, CRIACAO_USUARIO, DESATIVACAO_USUARIO, ALTERACAO_CONFIGURACAO, BACKUP, RESTAURACAO }

public sealed class Auditoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Entidade { get; set; }
    public Guid EntidadeId { get; set; }
    public OperacaoAuditoria Operacao { get; set; }
    public string? DadosAnteriores { get; set; }
    public string? DadosNovos { get; set; }
    public Guid? UsuarioId { get; set; }
    public DateTimeOffset Data { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Configuracao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Chave { get; set; }
    public required string Valor { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PerfilSaudeFinanceira : EntityBase
{
    public Guid? MembroId { get; set; }
    public decimal MetaReservaMeses { get; set; } = 6m;
    public decimal TetoComprometimentoPercentual { get; set; } = 30m;
    public decimal? MetaPoupancaPercentual { get; set; } = 20m;
    public string? Observacao { get; set; }
    public string CategoriasEssenciaisJson { get; set; } = "[]";
    public Membro? Membro { get; set; }
}

public sealed class ConfiguracaoFamilia
{
    public Guid FamiliaId { get; set; }
    public string Moeda { get; set; } = "BRL";
    public string Timezone { get; set; } = "America/Cuiaba";
    public string Locale { get; set; } = "pt-BR";
    public int PrimeiroDiaSemana { get; set; } = 1;
    public int InicioMesFinanceiro { get; set; } = 1;
    public Familia? Familia { get; set; }
}

public sealed class PreferenciaUsuario
{
    public Guid UsuarioId { get; set; }
    public string Idioma { get; set; } = "pt-BR";
    public string FormatoData { get; set; } = "dd/MM/yyyy";
    public string PaginaInicial { get; set; } = "dashboard";
    public int ItensPorPagina { get; set; } = 20;
    public bool ConfirmarOperacoes { get; set; } = true;
    public string PeriodoPadraoDashboard { get; set; } = "MES_ATUAL";
    public string Tema { get; set; } = "sistema";
    public Usuario? Usuario { get; set; }
}

public sealed class ConfiguracaoNotificacao
{
    public Guid UsuarioId { get; set; }
    public bool AvisarContasProximas { get; set; } = true;
    public int AntecedenciaDias { get; set; } = 3;
    public bool AvisarFatura { get; set; } = true;
    public bool AvisarSaldoProjetadoNegativo { get; set; } = true;
    public bool AvisarOrcamento { get; set; } = true;
    public Usuario? Usuario { get; set; }
}

public enum TipoRateio { PERCENTUAL, VALOR_FIXO, IGUALITARIO }

public sealed class RateioDespesa : EntityBase
{
    public Guid TransacaoId { get; set; }
    public Guid? LancamentoId { get; set; }
    public TipoRateio Tipo { get; set; }
    public long ValorDespesaCentavos { get; set; }
    public bool PadraoAplicado { get; set; }
    public Transacao? Transacao { get; set; }
    public ICollection<RateioDespesaItem> Itens { get; set; } = new List<RateioDespesaItem>();
}

public sealed class RateioDespesaItem : EntityBase
{
    public Guid RateioDespesaId { get; set; }
    public Guid MembroId { get; set; }
    public decimal Percentual { get; set; }
    public long ValorCentavos { get; set; }
    public RateioDespesa? RateioDespesa { get; set; }
    public Membro? Membro { get; set; }
}

public enum TipoConexaoFinanceira { MANUAL, OFX, CSV, OPEN_FINANCE }
public enum StatusConexaoFinanceira { CONFIGURADA, INATIVA, ERRO }

public sealed class ConexaoFinanceira : EntityBase
{
    public required string Instituicao { get; set; }
    public TipoConexaoFinanceira Tipo { get; set; } = TipoConexaoFinanceira.MANUAL;
    public StatusConexaoFinanceira Status { get; set; } = StatusConexaoFinanceira.INATIVA;
    public string? Configuracao { get; set; }
}

public enum StatusImportacao { PREVIA, VALIDADA, CONCLUIDA, COM_ERROS, CANCELADA }

public sealed class Importacao : EntityBase
{
    public required string NomeArquivo { get; set; }
    public StatusImportacao Status { get; set; } = StatusImportacao.PREVIA;
    public int TotalItens { get; set; }
    public int ItensValidos { get; set; }
    public int ItensComErro { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
    public ICollection<ImportacaoArquivo> Arquivos { get; set; } = new List<ImportacaoArquivo>();
    public ICollection<ImportacaoItem> Itens { get; set; } = new List<ImportacaoItem>();
}

public sealed class ImportacaoArquivo : EntityBase
{
    public Guid ImportacaoId { get; set; }
    public required string Nome { get; set; }
    public required string Tipo { get; set; }
    public long TamanhoBytes { get; set; }
    public string? Hash { get; set; }
    public Importacao? Importacao { get; set; }
}

public sealed class ImportacaoItem : EntityBase
{
    public Guid ImportacaoId { get; set; }
    public int Linha { get; set; }
    public required string Dados { get; set; }
    public string? HashDedupe { get; set; }
    public bool Valido { get; set; }
    public string? Erro { get; set; }
    public Importacao? Importacao { get; set; }
}
