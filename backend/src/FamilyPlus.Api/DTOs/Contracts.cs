using FamilyPlus.Api.Entities;

namespace FamilyPlus.Api.DTOs;

public record ApiResponse<T>(bool Success, T? Data = default, string? Message = null, IReadOnlyList<string>? Errors = null)
{
    public static ApiResponse<T> Ok(T data, string? message = null) => new(true, data, message);
    public static ApiResponse<T> Fail(string message, params string[] errors) => new(false, default, message, errors);
}

public record SetupRequest(string Nome, string Login, string Senha);
public record LoginRequest(string Login, string Senha);
public record AuthResult(Guid UsuarioId, string Nome, string Token, DateTimeOffset ExpiraEm);
public record SessionUser(Guid Id, string Nome, string Login, Guid FamiliaId, Guid? MembroId, string Perfil, IReadOnlyList<string> Permissoes);

public record MemberRequest(string Nome, string? Descricao, TipoMembro Tipo = TipoMembro.TITULAR, DateTimeOffset? DataNascimento = null);
public record MemberResponse(Guid Id, string Nome, string? Descricao, TipoMembro Tipo, DateTimeOffset? DataNascimento, bool Ativo, DateTimeOffset CriadoEm, bool PossuiUsuario = false);

public record AccountRequest(Guid MembroId, string Nome, TipoConta Tipo, long SaldoInicialCentavos, string? Observacao);
public record AccountResponse(Guid Id, Guid MembroId, string MembroNome, string Nome, TipoConta Tipo, long SaldoInicialCentavos, string? Observacao, bool Ativo);

public record CategoryRequest(string Nome, TipoCategoria Tipo, Guid? CategoriaPaiId);
public record CategoryResponse(Guid Id, string Nome, TipoCategoria Tipo, Guid? CategoriaPaiId, string? CategoriaPaiNome, bool Ativo);

public record BackupResponse(string NomeArquivo, DateTimeOffset CriadoEm);

public record FamilyRequest(string Nome, string MoedaPadrao, string Timezone);
public record FamilyResponse(Guid Id, string Nome, string MoedaPadrao, string Timezone, bool Ativo);
public record UserRequest(Guid MembroId, string Login, string Senha, string Perfil, bool Ativo = true);
public record UserResponse(Guid Id, Guid? MembroId, string? MembroNome, string Nome, string Login, bool Ativo, string Perfil, DateTimeOffset? UltimoAcessoEm);
public record PermissionResponse(Guid Id, string Codigo, string Modulo, string Descricao);
public record ScopeRequest(string Recurso, TipoEscopo TipoEscopo, IReadOnlyList<Guid>? MembroIds = null);
public record UserPermissionsResponse(Guid UsuarioId, IReadOnlyList<PermissionResponse> Permissoes, IReadOnlyList<ScopeRequest> Escopos);
public record FinancialProfileResponse(Guid MembroId, string MembroNome, long ContasCentavos, long ReceitasMesCentavos, long DespesasMesCentavos, long CartaoCentavos, decimal OrcamentoUtilizadoPercentual);
public record SplitItemRequest(Guid MembroId, decimal Percentual = 0, long ValorCentavos = 0);
public record SplitRequest(TipoRateio Tipo, IReadOnlyList<SplitItemRequest> Itens, bool AplicarComoPadrao = false);
public record SplitItemResponse(Guid MembroId, string MembroNome, decimal Percentual, long ValorCentavos);
public record SplitResponse(Guid Id, Guid TransacaoId, TipoRateio Tipo, long ValorDespesaCentavos, bool PadraoAplicado, IReadOnlyList<SplitItemResponse> Itens);
public record FamilyConfigurationResponse(Guid FamiliaId, string Moeda, string Timezone, string Locale, int PrimeiroDiaSemana, int InicioMesFinanceiro);
public record UserPreferencesResponse(Guid UsuarioId, string Idioma, string FormatoData, string PaginaInicial, int ItensPorPagina, bool ConfirmarOperacoes, string PeriodoPadraoDashboard, string Tema);
public record NotificationSettingsResponse(Guid UsuarioId, bool AvisarContasProximas, int AntecedenciaDias, bool AvisarFatura, bool AvisarSaldoProjetadoNegativo, bool AvisarOrcamento);
public record FamilySettingsResponse(FamilyResponse Familia, FamilyConfigurationResponse Configuracao, UserPreferencesResponse Preferencias, NotificationSettingsResponse Notificacoes);
public record SecurityPasswordRequest(string SenhaAtual, string NovaSenha);
public record SystemHealthResponse(string Application, string Version, string Mode, string Database, int Schema, string DataDirectory, string? UltimoBackup, bool IntegrityOk, string IntegrityMessage);
public record RestoreRequest(string NomeArquivo);
public record ConnectionRequest(string Instituicao, TipoConexaoFinanceira Tipo, string? Configuracao);
public record ConnectionResponse(Guid Id, string Instituicao, TipoConexaoFinanceira Tipo, StatusConexaoFinanceira Status, string? Configuracao);

public record TransactionRequest(TipoTransacao Tipo, string Descricao, long ValorCentavos, DateTimeOffset DataCompetencia, DateTimeOffset DataMovimentacao, Guid ContaId, Guid CategoriaId, Guid MembroId, StatusTransacao Status, string? Observacao);
public record TransactionResponse(Guid Id, Guid MembroId, string MembroNome, Guid ContaId, string ContaNome, Guid? CategoriaId, string? CategoriaNome, TipoTransacao Tipo, string Descricao, long ValorCentavos, DateTimeOffset DataCompetencia, DateTimeOffset DataMovimentacao, StatusTransacao Status, string? Observacao, Guid? TransferenciaId);
public record TransactionQueryRequest(string? DataInicio = null, string? DataFim = null, Guid? ContaId = null, Guid? MembroId = null, Guid? CategoriaId = null, string? Tipo = null, string? Status = null, string? Texto = null, string? OrdenarPor = "data", bool Desc = true, int Pagina = 1, int TamanhoPagina = 20);
public record TransactionPage(IReadOnlyList<TransactionResponse> Items, int Pagina, int TamanhoPagina, int TotalItens, int TotalPaginas);
public record FinanceSummary(long ReceitasCentavos, long DespesasCentavos, long ResultadoCentavos);
public record AccountBalanceResponse(Guid ContaId, string ContaNome, long SaldoInicialCentavos, long EntradasCentavos, long SaidasCentavos, long SaldoAtualCentavos);
public record AccountStatementResponse(AccountBalanceResponse Resumo, IReadOnlyList<TransactionResponse> Movimentacoes);

public record LedgerRequest(TipoLancamento Tipo, NaturezaLancamento Natureza, StatusLancamento Status, OrigemLancamento OrigemTipo, Guid? OrigemId, string Descricao, long ValorCentavos, DateTimeOffset DataCompetencia, DateTimeOffset? DataVencimento, DateTimeOffset? DataEfetivacao, Guid ContaId, Guid? CategoriaId, Guid MembroId, long? ValorPrevistoCentavos, string? Observacao);
public record LedgerResponse(Guid Id, Guid FamiliaId, Guid MembroId, string MembroNome, Guid ContaId, string ContaNome, Guid? CategoriaId, string? CategoriaNome, TipoLancamento Tipo, NaturezaLancamento Natureza, StatusLancamento Status, OrigemLancamento OrigemTipo, Guid? OrigemId, string Descricao, long ValorCentavos, long? ValorPrevistoCentavos, DateTimeOffset DataCompetencia, DateTimeOffset? DataVencimento, DateTimeOffset? DataEfetivacao, Guid? CriadoPorUsuarioId, string? Observacao);
public record LedgerQueryRequest(string? DataInicio = null, string? DataFim = null, Guid? ContaId = null, Guid? MembroId = null, Guid? CategoriaId = null, TipoLancamento? Tipo = null, NaturezaLancamento? Natureza = null, StatusLancamento? Status = null, OrigemLancamento? OrigemTipo = null, string? Texto = null, int Pagina = 1, int TamanhoPagina = 50);
public record LedgerPage(IReadOnlyList<LedgerResponse> Items, int Pagina, int TamanhoPagina, int TotalItens, int TotalPaginas);
public record LedgerBalanceResponse(Guid ContaId, string ContaNome, long SaldoInicialCentavos, long EntradasCentavos, long SaidasCentavos, long SaldoAtualCentavos, long SaldoProjetadoCentavos, DateTimeOffset? DataReferencia);
public record LedgerStatementResponse(LedgerBalanceResponse Resumo, IReadOnlyList<LedgerResponse> Lancamentos);
public record AdjustmentRequest(Guid ContaId, Guid MembroId, long ValorCentavos, DateTimeOffset Data, string Justificativa, StatusLancamento Status = StatusLancamento.EFETIVADO);
public record LedgerEffectRequest(long? ValorCentavos = null, DateTimeOffset? DataEfetivacao = null);
public record TransferRequest(Guid ContaOrigemId, Guid ContaDestinoId, Guid MembroId, long ValorCentavos, DateTimeOffset Data, string Descricao, StatusTransacao Status, string? Observacao);
public record TransferResponse(Guid Id, Guid ContaOrigemId, string ContaOrigemNome, Guid ContaDestinoId, string ContaDestinoNome, Guid MembroId, string MembroNome, long ValorCentavos, DateTimeOffset Data, string Descricao, StatusTransacao Status, IReadOnlyList<Guid> TransacaoIds);

public record CardRequest(Guid MembroId, string Nome, string Bandeira, string UltimosDigitos, long LimiteTotalCentavos, int DiaFechamento, int DiaVencimento, Guid ContaPagamentoPadraoId, string? Observacao);
public record CardResponse(Guid Id, Guid MembroId, string MembroNome, string Nome, string Bandeira, string UltimosDigitos, long LimiteTotalCentavos, int DiaFechamento, int DiaVencimento, Guid ContaPagamentoPadraoId, string ContaPagamentoPadraoNome, string? Observacao, bool Ativo);
public record CardLimitResponse(Guid CartaoId, long LimiteTotalCentavos, long LimiteUtilizadoCentavos, long LimiteDisponivelCentavos, decimal PercentualUtilizado);
public record CardPurchaseRequest(Guid CartaoId, Guid MembroId, Guid CategoriaId, string Descricao, long ValorTotalCentavos, int QuantidadeParcelas, DateTimeOffset DataCompra, string? Observacao);
public record CardPurchaseResponse(Guid Id, Guid CartaoId, string CartaoNome, Guid MembroId, string MembroNome, Guid CategoriaId, string CategoriaNome, string Descricao, long ValorTotalCentavos, int QuantidadeParcelas, DateTimeOffset DataCompra, string? Observacao, StatusCompraCartao Status, IReadOnlyList<InstallmentResponse> Parcelas);
public record InstallmentResponse(Guid Id, int NumeroParcela, int QuantidadeTotal, long ValorCentavos, DateTimeOffset DataCompetencia, Guid FaturaId, StatusCompraCartao Status);
public record InvoiceResponse(Guid Id, Guid CartaoId, string CartaoNome, DateTimeOffset Competencia, DateTimeOffset DataFechamento, DateTimeOffset DataVencimento, long ValorTotalCentavos, StatusFatura Status, DateTimeOffset? DataPagamento, Guid? ContaPagamentoId, string? ContaPagamentoNome);
public record InvoiceDetailResponse(InvoiceResponse Fatura, IReadOnlyList<InvoiceItemResponse> Itens);
public record InvoiceItemResponse(Guid ParcelaId, Guid CompraId, string Descricao, string CategoriaNome, int NumeroParcela, int QuantidadeTotal, long ValorCentavos, StatusCompraCartao Status);
public record InvoicePaymentRequest(Guid ContaPagamentoId, DateTimeOffset DataPagamento, long ValorCentavos);
public record RefundRequest(long ValorCentavos, DateTimeOffset Data, string Descricao, Guid? FaturaId);
public record InvoiceProjectionResponse(DateTimeOffset Competencia, long ValorCentavos, StatusFatura Status);

public record RecurrenceRequest(Guid MembroId, Guid ContaId, Guid CategoriaId, TipoTransacao Tipo, string Descricao, long ValorCentavos, FrequenciaRecorrencia Frequencia, DateTimeOffset DataInicio, DateTimeOffset? DataFim, int? DiaReferencia, bool GerarAutomaticamente, bool ValorVariavel, TipoRecorrencia Classificacao, string? Observacao);
public record RecurrenceResponse(Guid Id, Guid MembroId, string MembroNome, Guid ContaId, string ContaNome, Guid CategoriaId, string CategoriaNome, TipoTransacao Tipo, string Descricao, long ValorCentavos, FrequenciaRecorrencia Frequencia, DateTimeOffset DataInicio, DateTimeOffset? DataFim, DateTimeOffset ProximaOcorrencia, int DiaReferencia, bool GerarAutomaticamente, bool ValorVariavel, TipoRecorrencia Classificacao, StatusRecorrencia Status, string? Observacao, bool Ativo);
public record RecurrenceSummaryResponse(long ReceitasMensaisCentavos, long DespesasMensaisCentavos, long SaldoMensalCentavos, long SaldoRealCentavos, long SaldoProjetadoCentavos);
public record RecurrenceOccurrenceResponse(Guid Id, Guid RecorrenciaId, string Descricao, TipoTransacao Tipo, TipoRecorrencia Classificacao, DateTimeOffset DataPrevista, long ValorPrevistoCentavos, long? ValorRealizadoCentavos, StatusOcorrenciaRecorrencia Status, Guid? TransacaoId, Guid? CompraCartaoId, string? Observacao);
public record OccurrenceQueryRequest(DateTimeOffset? DataInicio = null, DateTimeOffset? DataFim = null, Guid? RecorrenciaId = null, StatusOcorrenciaRecorrencia? Status = null, TipoTransacao? Tipo = null);
public record EffectOccurrenceRequest(long ValorRealizadoCentavos, DateTimeOffset? DataEfetivacao, string? Observacao);
public record EditOccurrenceRequest(long ValorPrevistoCentavos, DateTimeOffset? DataPrevista, string? Observacao);

public record SubscriptionRequest(Guid MembroId, string Nome, Guid CategoriaId, Guid ContaId, long ValorCentavos, PeriodicidadeAssinatura Periodicidade, DateTimeOffset DataInicio, DateTimeOffset ProximaCobranca, MetodoPagamentoAssinatura MetodoPagamento, Guid? CartaoId, string? Observacao);
public record SubscriptionResponse(Guid Id, Guid MembroId, string MembroNome, string Nome, Guid CategoriaId, string CategoriaNome, Guid ContaId, string ContaNome, long ValorCentavos, PeriodicidadeAssinatura Periodicidade, DateTimeOffset DataInicio, DateTimeOffset ProximaCobranca, MetodoPagamentoAssinatura MetodoPagamento, Guid? CartaoId, string? CartaoNome, bool Ativa, DateTimeOffset? DataCancelamento, string? Observacao, long CustoMensalEquivalenteCentavos, long CustoAnualEquivalenteCentavos);
public record SubscriptionSummaryResponse(long CustoMensalCentavos, long CustoAnualCentavos, int AssinaturasAtivas);

public record DashboardQueryRequest(string? DataInicio = null, string? DataFim = null, Guid? MembroId = null, int HorizonteDias = 30);
public record DashboardAccountResponse(Guid ContaId, string ContaNome, string MembroNome, long SaldoAtualCentavos);
public record DashboardBalanceResponse(long SaldoConsolidadoCentavos, IReadOnlyList<DashboardAccountResponse> Contas);
public record DashboardPeriodResponse(long ReceitasCentavos, long DespesasCentavos, long ResultadoCentavos);
public record DashboardProjectionResponse(long SaldoAtualCentavos, long ReceitasPrevistasCentavos, long DespesasPrevistasCentavos, long FaturasPrevistasCentavos, long SaldoProjetadoCentavos, int HorizonteDias, DateTimeOffset DataInicio, DateTimeOffset DataFim);
public record DashboardCashFlowRow(DateTimeOffset Data, long EntradasRealizadasCentavos, long SaidasRealizadasCentavos, long EntradasPrevistasCentavos, long SaidasPrevistasCentavos, long FaturasCentavos, long SaldoProjetadoCentavos);
public record DashboardCashFlowResponse(DateTimeOffset DataInicio, DateTimeOffset DataFim, long SaldoInicialCentavos, IReadOnlyList<DashboardCashFlowRow> Itens);
public record DashboardUpcomingResponse(Guid Id, string Origem, string Descricao, DateTimeOffset Data, long ValorCentavos, TipoTransacao Tipo, bool Previsto, string? Referencia);
public record DashboardCardResponse(Guid CartaoId, string Nome, string Bandeira, long FaturaAtualCentavos, DateTimeOffset? Vencimento, long LimiteTotalCentavos, long LimiteUtilizadoCentavos, long LimiteDisponivelCentavos, decimal PercentualUtilizado);
public record DashboardCategoryResponse(Guid? CategoriaId, string CategoriaNome, long ValorCentavos, decimal Percentual);
public record DashboardMonthlyResponse(DateTimeOffset Competencia, long ReceitasCentavos, long DespesasCentavos, long ResultadoCentavos, long SaldoFinalCentavos);
public record DashboardKpiResponse(decimal TaxaPoupancaPercentual, decimal ComprometimentoRendaPercentual, decimal GastosFixosPercentual, long ProximasDespesasCentavos, long ProximasReceitasCentavos, int AlertasAbertos);
public record DashboardSummaryResponse(DashboardBalanceResponse Saldo, DashboardPeriodResponse Periodo, DashboardProjectionResponse Projecao, DashboardCashFlowResponse FluxoCaixa, IReadOnlyList<DashboardUpcomingResponse> ProximasReceitas, IReadOnlyList<DashboardUpcomingResponse> ProximasDespesas, IReadOnlyList<DashboardCardResponse> Cartoes, IReadOnlyList<DashboardCategoryResponse> DespesasCategorias, IReadOnlyList<DashboardCategoryResponse> ReceitasCategorias, IReadOnlyList<DashboardMonthlyResponse> Evolucao, DashboardKpiResponse Kpis, BudgetDashboardResponse? Orcamento = null, FinancialHealthResponse? SaudeFinanceira = null);

public record FinancialHealthProfileRequest(Guid? MembroId, decimal MetaReservaMeses, decimal TetoComprometimentoPercentual, decimal? MetaPoupancaPercentual, string? Observacao, IReadOnlyList<Guid> CategoriasEssenciais);
public record FinancialHealthProfileResponse(Guid? MembroId, decimal MetaReservaMeses, decimal TetoComprometimentoPercentual, decimal? MetaPoupancaPercentual, string? Observacao, IReadOnlyList<Guid> CategoriasEssenciais, bool Configurado);
public record FinancialHealthMetric(decimal? Valor, string Estado);
public record FinancialHealthResponse(FinancialHealthProfileResponse Perfil, FinancialHealthMetric TaxaPoupanca, FinancialHealthMetric ReservaEmergenciaMeses, FinancialHealthMetric ComprometimentoRenda, FinancialHealthMetric GastosFixos, FinancialHealthMetric CoberturaOrcamentaria);

public record AlertResponse(Guid Id, TipoAlerta Tipo, SeveridadeAlerta Severidade, string Titulo, string Mensagem, string EntidadeOrigem, Guid? EntidadeId, DateTimeOffset DataGeracao, DateTimeOffset? DataLeitura, bool Resolvido);
public record AlertQueryRequest(TipoAlerta? Tipo = null, SeveridadeAlerta? Severidade = null, bool? Resolvido = false);

public record BudgetRequest(int Ano, int Mes, Guid? MembroId, string Descricao, long ReceitaPrevistaCentavos, ModoReceitaPrevista ModoReceitaPrevista, StatusOrcamento Status, string? Observacao);
public record BudgetResponse(Guid Id, int Ano, int Mes, Guid? MembroId, string? MembroNome, string Descricao, long ReceitaPrevistaCentavos, ModoReceitaPrevista ModoReceitaPrevista, StatusOrcamento Status, string? Observacao, DateTimeOffset CriadoEm, DateTimeOffset AtualizadoEm);
public record BudgetItemRequest(Guid CategoriaId, Guid? MembroId, long ValorPlanejadoCentavos, string? Observacao);
public record BudgetItemResponse(Guid Id, Guid OrcamentoId, Guid CategoriaId, string CategoriaNome, Guid? MembroId, string? MembroNome, long ValorPlanejadoCentavos, string? Observacao);
public record BudgetLineResponse(Guid? ItemId, Guid CategoriaId, string CategoriaNome, Guid? MembroId, string? MembroNome, long ValorPlanejadoCentavos, long RealizadoCentavos, long DisponivelCentavos, decimal PercentualUtilizado, long VariacaoCentavos, decimal VariacaoPercentual, long ProjetadoCentavos, decimal PercentualProjetado, string Situacao, bool SemOrcamento);
public record BudgetSummaryResponse(BudgetResponse Orcamento, long TotalPlanejadoCentavos, long TotalRealizadoCentavos, long SaldoOrcamentarioCentavos, decimal PercentualUtilizado, long TotalProjetadoCentavos, decimal ComprometimentoProjetadoPercentual, long MargemPlanejadaCentavos, decimal PoupancaPlanejadaPercentual, IReadOnlyList<BudgetLineResponse> Linhas);
public record BudgetComparisonResponse(BudgetResponse Orcamento, IReadOnlyList<BudgetLineResponse> Linhas, long TotalPlanejadoCentavos, long TotalRealizadoCentavos, long VariacaoCentavos, decimal VariacaoPercentual);
public record BudgetProjectionResponse(BudgetResponse Orcamento, IReadOnlyList<BudgetLineResponse> Linhas, long TotalPlanejadoCentavos, long TotalRealizadoCentavos, long TotalProjetadoCentavos, long EstouroProjetadoCentavos);
public record BudgetCopyRequest(int Ano, int Mes, Guid? MembroId, string? Descricao, decimal ReajustePercentual = 0);
public record BudgetDashboardResponse(Guid OrcamentoId, int Ano, int Mes, long ReceitaPrevistaCentavos, long TotalPlanejadoCentavos, long TotalRealizadoCentavos, long SaldoDisponivelCentavos, decimal PercentualUtilizado, IReadOnlyList<BudgetLineResponse> TopCategorias);
