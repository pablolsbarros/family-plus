export type ApiEnvelope<T> = { success: boolean; data?: T; message?: string; errors?: string[] };

export type User = { id: string; nome: string; login: string; familiaId?: string; membroId?: string; perfil?: string; permissoes?: string[] };
export type AuthResult = { usuarioId: string; nome: string; token: string; expiraEm: string };
export type Member = { id: string; nome: string; descricao?: string; tipo?: 'TITULAR' | 'CONJUGE' | 'DEPENDENTE' | 'OUTRO'; dataNascimento?: string; ativo: boolean; criadoEm: string; possuiUsuario?: boolean };
export type Account = { id: string; membroId: string; membroNome: string; nome: string; tipo: AccountType; saldoInicialCentavos: number; observacao?: string; ativo: boolean };
export type AccountType = 'ContaCorrente' | 'ContaPoupanca' | 'Carteira' | 'Dinheiro' | 'ContaDigital' | 'Outros';
export type CategoryType = 'Receita' | 'Despesa';
export type Category = { id: string; nome: string; tipo: CategoryType; categoriaPaiId?: string; categoriaPaiNome?: string; ativo: boolean };
export type TransactionType = 'RECEITA' | 'DESPESA' | 'TRANSFERENCIA_ENTRADA' | 'TRANSFERENCIA_SAIDA';
export type TransactionStatus = 'PREVISTA' | 'EFETIVADA' | 'CANCELADA';
export type Transaction = { id: string; membroId: string; membroNome: string; contaId: string; contaNome: string; categoriaId?: string; categoriaNome?: string; tipo: TransactionType; descricao: string; valorCentavos: number; dataCompetencia: string; dataMovimentacao: string; status: TransactionStatus; observacao?: string; transferenciaId?: string };
export type TransactionPage = { items: Transaction[]; pagina: number; tamanhoPagina: number; totalItens: number; totalPaginas: number };
export type TransactionRequest = { tipo: TransactionType; descricao: string; valorCentavos: number; dataCompetencia: string; dataMovimentacao: string; contaId: string; categoriaId: string; membroId: string; status: TransactionStatus; observacao?: string };
export type FinanceSummary = { receitasCentavos: number; despesasCentavos: number; resultadoCentavos: number };
export type AccountBalance = { contaId: string; contaNome: string; saldoInicialCentavos: number; entradasCentavos: number; saidasCentavos: number; saldoAtualCentavos: number };
export type AccountStatement = { resumo: AccountBalance; movimentacoes: Transaction[] };
export type LedgerType = 'RECEITA' | 'DESPESA' | 'TRANSFERENCIA_ENTRADA' | 'TRANSFERENCIA_SAIDA' | 'PAGAMENTO_FATURA' | 'AJUSTE';
export type LedgerNature = 'CONSUMO' | 'RENDA' | 'TRANSFERENCIA' | 'PAGAMENTO_FATURA' | 'AJUSTE';
export type LedgerStatus = 'PREVISTO' | 'EFETIVADO' | 'CANCELADO';
export type LedgerOrigin = 'MANUAL' | 'TRANSFERENCIA' | 'COMPRA_CARTAO' | 'FATURA' | 'RECORRENCIA' | 'ASSINATURA' | 'IMPORTACAO' | 'AJUSTE';
export type Ledger = { id: string; familiaId: string; membroId: string; membroNome: string; contaId: string; contaNome: string; categoriaId?: string; categoriaNome?: string; tipo: LedgerType; natureza: LedgerNature; status: LedgerStatus; origemTipo: LedgerOrigin; origemId?: string; descricao: string; valorCentavos: number; valorPrevistoCentavos?: number; dataCompetencia: string; dataVencimento?: string; dataEfetivacao?: string; criadoPorUsuarioId?: string; observacao?: string };
export type LedgerPage = { items: Ledger[]; pagina: number; tamanhoPagina: number; totalItens: number; totalPaginas: number };
export type LedgerBalance = { contaId: string; contaNome: string; saldoInicialCentavos: number; entradasCentavos: number; saidasCentavos: number; saldoAtualCentavos: number; saldoProjetadoCentavos: number; dataReferencia?: string };
export type LedgerRequest = { tipo: LedgerType; natureza: LedgerNature; status: LedgerStatus; origemTipo: LedgerOrigin; origemId?: string; descricao: string; valorCentavos: number; valorPrevistoCentavos?: number; dataCompetencia: string; dataVencimento?: string; dataEfetivacao?: string; contaId: string; categoriaId?: string; membroId: string; observacao?: string };
export type Transfer = { id: string; contaOrigemId: string; contaOrigemNome: string; contaDestinoId: string; contaDestinoNome: string; membroId: string; membroNome: string; valorCentavos: number; data: string; descricao: string; status: TransactionStatus; transacaoIds: string[] };
export type TransferRequest = { contaOrigemId: string; contaDestinoId: string; membroId: string; valorCentavos: number; data: string; descricao: string; status: TransactionStatus; observacao?: string };
export type Card = { id: string; membroId: string; membroNome: string; nome: string; bandeira: string; ultimosDigitos: string; limiteTotalCentavos: number; diaFechamento: number; diaVencimento: number; contaPagamentoPadraoId: string; contaPagamentoPadraoNome: string; observacao?: string; ativo: boolean };
export type CardRequest = Omit<Card, 'id' | 'membroNome' | 'contaPagamentoPadraoNome' | 'ativo'>;
export type CardLimit = { cartaoId: string; limiteTotalCentavos: number; limiteUtilizadoCentavos: number; limiteDisponivelCentavos: number; percentualUtilizado: number };
export type PurchaseStatus = 'ATIVA' | 'CANCELADA' | 'ESTORNADA';
export type Installment = { id: string; numeroParcela: number; quantidadeTotal: number; valorCentavos: number; dataCompetencia: string; faturaId: string; status: PurchaseStatus };
export type CardPurchase = { id: string; cartaoId: string; cartaoNome: string; membroId: string; membroNome: string; categoriaId: string; categoriaNome: string; descricao: string; valorTotalCentavos: number; quantidadeParcelas: number; dataCompra: string; observacao?: string; status: PurchaseStatus; parcelas: Installment[] };
export type CardPurchaseRequest = { cartaoId: string; membroId: string; categoriaId: string; descricao: string; valorTotalCentavos: number; quantidadeParcelas: number; dataCompra: string; observacao?: string };
export type InvoiceStatus = 'ABERTA' | 'FECHADA' | 'PAGA' | 'VENCIDA' | 'CANCELADA';
export type Invoice = { id: string; cartaoId: string; cartaoNome: string; competencia: string; dataFechamento: string; dataVencimento: string; valorTotalCentavos: number; status: InvoiceStatus; dataPagamento?: string; contaPagamentoId?: string; contaPagamentoNome?: string };
export type InvoiceDetail = { fatura: Invoice; itens: Array<{ parcelaId: string; compraId: string; descricao: string; categoriaNome: string; numeroParcela: number; quantidadeTotal: number; valorCentavos: number; status: PurchaseStatus }> };
export type InvoiceProjection = { competencia: string; valorCentavos: number; status: InvoiceStatus };
export type RecurrenceFrequency = 'DIARIA' | 'SEMANAL' | 'QUINZENAL' | 'MENSAL' | 'BIMESTRAL' | 'TRIMESTRAL' | 'SEMESTRAL' | 'ANUAL';
export type RecurrenceStatus = 'ATIVA' | 'PAUSADA' | 'ENCERRADA';
export type RecurrenceClass = 'NORMAL' | 'CONTA_FIXA';
export type OccurrenceStatus = 'PENDENTE' | 'EFETIVADA' | 'IGNORADA' | 'ATRASADA' | 'CANCELADA';
export type Recurrence = { id: string; membroId: string; membroNome: string; contaId: string; contaNome: string; categoriaId: string; categoriaNome: string; tipo: TransactionType; descricao: string; valorCentavos: number; frequencia: RecurrenceFrequency; dataInicio: string; dataFim?: string; proximaOcorrencia: string; diaReferencia: number; gerarAutomaticamente: boolean; valorVariavel: boolean; classificacao: RecurrenceClass; status: RecurrenceStatus; observacao?: string; ativo: boolean };
export type RecurrenceRequest = Omit<Recurrence, 'id' | 'membroNome' | 'contaNome' | 'categoriaNome' | 'proximaOcorrencia' | 'diaReferencia' | 'status' | 'ativo'>;
export type RecurrenceSummary = { receitasMensaisCentavos: number; despesasMensaisCentavos: number; saldoMensalCentavos: number; saldoRealCentavos: number; saldoProjetadoCentavos: number };
export type RecurrenceOccurrence = { id: string; recorrenciaId: string; descricao: string; tipo: TransactionType; classificacao: RecurrenceClass; dataPrevista: string; valorPrevistoCentavos: number; valorRealizadoCentavos?: number; status: OccurrenceStatus; transacaoId?: string; compraCartaoId?: string; observacao?: string };
export type SubscriptionPeriodicity = 'MENSAL' | 'BIMESTRAL' | 'TRIMESTRAL' | 'SEMESTRAL' | 'ANUAL';
export type SubscriptionPaymentMethod = 'CONTA' | 'CARTAO';
export type Subscription = { id: string; membroId: string; membroNome: string; nome: string; categoriaId: string; categoriaNome: string; contaId: string; contaNome: string; valorCentavos: number; periodicidade: SubscriptionPeriodicity; dataInicio: string; proximaCobranca: string; metodoPagamento: SubscriptionPaymentMethod; cartaoId?: string; cartaoNome?: string; ativa: boolean; dataCancelamento?: string; observacao?: string; custoMensalEquivalenteCentavos: number; custoAnualEquivalenteCentavos: number };
export type SubscriptionRequest = Omit<Subscription, 'id' | 'membroNome' | 'categoriaNome' | 'contaNome' | 'cartaoNome' | 'ativa' | 'dataCancelamento' | 'custoMensalEquivalenteCentavos' | 'custoAnualEquivalenteCentavos'>;
export type SubscriptionSummary = { custoMensalCentavos: number; custoAnualCentavos: number; assinaturasAtivas: number };
export type DashboardQuery = { dataInicio?: string; dataFim?: string; membroId?: string; horizonteDias?: number };
export type DashboardAccount = { contaId: string; contaNome: string; membroNome: string; saldoAtualCentavos: number };
export type DashboardBalance = { saldoConsolidadoCentavos: number; contas: DashboardAccount[] };
export type DashboardPeriod = { receitasCentavos: number; despesasCentavos: number; resultadoCentavos: number };
export type DashboardProjection = { saldoAtualCentavos: number; receitasPrevistasCentavos: number; despesasPrevistasCentavos: number; faturasPrevistasCentavos: number; saldoProjetadoCentavos: number; horizonteDias: number; dataInicio: string; dataFim: string };
export type DashboardCashFlowRow = { data: string; entradasRealizadasCentavos: number; saidasRealizadasCentavos: number; entradasPrevistasCentavos: number; saidasPrevistasCentavos: number; faturasCentavos: number; saldoProjetadoCentavos: number };
export type DashboardCashFlow = { dataInicio: string; dataFim: string; saldoInicialCentavos: number; itens: DashboardCashFlowRow[] };
export type DashboardUpcoming = { id: string; origem: string; descricao: string; data: string; valorCentavos: number; tipo: TransactionType; previsto: boolean; referencia?: string };
export type DashboardCard = { cartaoId: string; nome: string; bandeira: string; faturaAtualCentavos: number; vencimento?: string; limiteTotalCentavos: number; limiteUtilizadoCentavos: number; limiteDisponivelCentavos: number; percentualUtilizado: number };
export type DashboardCategory = { categoriaId?: string; categoriaNome: string; valorCentavos: number; percentual: number };
export type DashboardMonthly = { competencia: string; receitasCentavos: number; despesasCentavos: number; resultadoCentavos: number; saldoFinalCentavos: number };
export type DashboardKpis = { taxaPoupancaPercentual: number; comprometimentoRendaPercentual: number; gastosFixosPercentual: number; proximasDespesasCentavos: number; proximasReceitasCentavos: number; alertasAbertos: number };
export type DashboardSummary = { saldo: DashboardBalance; periodo: DashboardPeriod; projecao: DashboardProjection; fluxoCaixa: DashboardCashFlow; proximasReceitas: DashboardUpcoming[]; proximasDespesas: DashboardUpcoming[]; cartoes: DashboardCard[]; despesasCategorias: DashboardCategory[]; receitasCategorias: DashboardCategory[]; evolucao: DashboardMonthly[]; kpis: DashboardKpis; orcamento?: BudgetDashboard | null };
export type AlertType = 'CONTA_VENCIDA' | 'CONTA_PROXIMA' | 'FATURA_PROXIMA' | 'FATURA_VENCIDA' | 'SALDO_NEGATIVO' | 'SALDO_PROJETADO_NEGATIVO' | 'LIMITE_CARTAO_ALTO' | 'RECORRENCIA_ATRASADA' | 'ORCAMENTO_ATENCAO' | 'ORCAMENTO_CRITICO' | 'ORCAMENTO_ESTOURADO' | 'ORCAMENTO_ESTOURO_PROJETADO' | 'CATEGORIA_SEM_ORCAMENTO';
export type AlertSeverity = 'INFORMACAO' | 'ATENCAO' | 'CRITICO';
export type FinancialAlert = { id: string; tipo: AlertType; severidade: AlertSeverity; titulo: string; mensagem: string; entidadeOrigem: string; entidadeId?: string; dataGeracao: string; dataLeitura?: string; resolvido: boolean };
export type BudgetStatus = 'RASCUNHO' | 'ATIVO' | 'ENCERRADO';
export type BudgetRevenueMode = 'MANUAL' | 'RECORRENCIAS';
export type Budget = { id: string; ano: number; mes: number; membroId?: string; membroNome?: string; descricao: string; receitaPrevistaCentavos: number; modoReceitaPrevista: BudgetRevenueMode; status: BudgetStatus; observacao?: string; criadoEm: string; atualizadoEm: string };
export type BudgetRequest = { ano: number; mes: number; membroId?: string; descricao: string; receitaPrevistaCentavos: number; modoReceitaPrevista: BudgetRevenueMode; status: BudgetStatus; observacao?: string };
export type BudgetItem = { id: string; orcamentoId: string; categoriaId: string; categoriaNome: string; membroId?: string; membroNome?: string; valorPlanejadoCentavos: number; observacao?: string };
export type BudgetItemRequest = { categoriaId: string; membroId?: string; valorPlanejadoCentavos: number; observacao?: string };
export type BudgetLine = { itemId?: string; categoriaId: string; categoriaNome: string; membroId?: string; membroNome?: string; valorPlanejadoCentavos: number; realizadoCentavos: number; disponivelCentavos: number; percentualUtilizado: number; variacaoCentavos: number; variacaoPercentual: number; projetadoCentavos: number; percentualProjetado: number; situacao: 'SAUDAVEL' | 'ATENCAO' | 'CRITICO' | 'ESTOURADO' | 'SEM_ORCAMENTO'; semOrcamento: boolean };
export type BudgetSummary = { orcamento: Budget; totalPlanejadoCentavos: number; totalRealizadoCentavos: number; saldoOrcamentarioCentavos: number; percentualUtilizado: number; totalProjetadoCentavos: number; comprometimentoProjetadoPercentual: number; margemPlanejadaCentavos: number; poupancaPlanejadaPercentual: number; linhas: BudgetLine[] };
export type BudgetComparison = { orcamento: Budget; linhas: BudgetLine[]; totalPlanejadoCentavos: number; totalRealizadoCentavos: number; variacaoCentavos: number; variacaoPercentual: number };
export type BudgetProjection = { orcamento: Budget; linhas: BudgetLine[]; totalPlanejadoCentavos: number; totalRealizadoCentavos: number; totalProjetadoCentavos: number; estouroProjetadoCentavos: number };
export type BudgetDashboard = { orcamentoId: string; ano: number; mes: number; receitaPrevistaCentavos: number; totalPlanejadoCentavos: number; totalRealizadoCentavos: number; saldoDisponivelCentavos: number; percentualUtilizado: number; topCategorias: BudgetLine[] };
export type Family = { id: string; nome: string; moedaPadrao: string; timezone: string; ativo: boolean };
export type FamilySettings = { familia: Family; configuracao: { familiaId: string; moeda: string; timezone: string; locale: string; primeiroDiaSemana: number; inicioMesFinanceiro: number }; preferencias: UserPreferences; notificacoes: NotificationSettings };
export type UserPreferences = { usuarioId: string; idioma: string; formatoData: string; paginaInicial: string; itensPorPagina: number; confirmarOperacoes: boolean; periodoPadraoDashboard: string; tema: string };
export type NotificationSettings = { usuarioId: string; avisarContasProximas: boolean; antecedenciaDias: number; avisarFatura: boolean; avisarSaldoProjetadoNegativo: boolean; avisarOrcamento: boolean };
export type ManagedUser = { id: string; membroId?: string; membroNome?: string; nome: string; login: string; ativo: boolean; perfil: string; ultimoAcessoEm?: string };
export type FinancialProfile = { membroId: string; membroNome: string; contasCentavos: number; receitasMesCentavos: number; despesasMesCentavos: number; cartaoCentavos: number; orcamentoUtilizadoPercentual: number };
export type Permission = { id: string; codigo: string; modulo: string; descricao: string };
export type Scope = { recurso: string; tipoEscopo: 'PROPRIOS' | 'FAMILIA' | 'SELECIONADOS'; membroIds?: string[] };
export type UserPermissions = { usuarioId: string; permissoes: Permission[]; escopos: Scope[] };
export type Split = { id: string; transacaoId: string; tipo: 'PERCENTUAL' | 'VALOR_FIXO' | 'IGUALITARIO'; valorDespesaCentavos: number; padraoAplicado: boolean; itens: Array<{ membroId: string; membroNome: string; percentual: number; valorCentavos: number }> };
export type SystemHealth = { application: string; version: string; mode: string; database: string; schema: number; dataDirectory: string; ultimoBackup?: string; integrityOk: boolean; integrityMessage: string };

const baseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5160/api';
let sessionToken = localStorage.getItem('familyplus.session') ?? '';

export function setSession(token: string) {
  sessionToken = token;
  if (token) localStorage.setItem('familyplus.session', token);
  else localStorage.removeItem('familyplus.session');
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...(sessionToken ? { 'X-Session-Token': sessionToken } : {}), ...options.headers },
  });
  const body = (await response.json()) as ApiEnvelope<T>;
  if (!response.ok || !body.success) throw new Error(body.errors?.join(' ') || body.message || 'Não foi possível concluir a operação.');
  return body.data as T;
}

export const api = {
  setupStatus: () => request<{ configured: boolean }>('/auth/setup-status'),
  setup: (payload: { nome: string; login: string; senha: string }) => request<AuthResult>('/auth/setup', { method: 'POST', body: JSON.stringify(payload) }),
  login: (payload: { login: string; senha: string }) => request<AuthResult>('/auth/login', { method: 'POST', body: JSON.stringify(payload) }),
  logout: () => request<object>('/auth/logout', { method: 'POST' }),
  me: () => request<User>('/auth/me'),
  members: () => request<Member[]>('/membros'),
  createMember: (payload: { nome: string; descricao?: string; tipo?: Member['tipo']; dataNascimento?: string }) => request<Member>('/membros', { method: 'POST', body: JSON.stringify(payload) }),
  updateMember: (id: string, payload: { nome: string; descricao?: string; tipo?: Member['tipo']; dataNascimento?: string }) => request<Member>(`/membros/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  memberStatus: (id: string, ativo: boolean) => request<Member>(`/membros/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativo) }),
  family: () => request<Family>('/familia'),
  updateFamily: (payload: { nome: string; moedaPadrao: string; timezone: string }) => request<Family>('/familia', { method: 'PUT', body: JSON.stringify(payload) }),
  users: () => request<ManagedUser[]>('/usuarios'),
  createUser: (payload: { membroId: string; login: string; senha: string; perfil: string; ativo?: boolean }) => request<ManagedUser>('/usuarios', { method: 'POST', body: JSON.stringify(payload) }),
  userStatus: (id: string, ativo: boolean) => request<ManagedUser>(`/usuarios/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativo) }),
  permissions: () => request<Permission[]>('/perfis'),
  userPermissions: (id: string) => request<UserPermissions>(`/usuarios/${id}/permissoes`),
  updateUserPermissions: (id: string, payload: { perfil: string; escopos: Scope[] }) => request<object>(`/usuarios/${id}/permissoes`, { method: 'PUT', body: JSON.stringify(payload) }),
  financialProfiles: () => request<FinancialProfile[]>('/familia/perfil-financeiro'),
  accounts: () => request<Account[]>('/contas'),
  createAccount: (payload: Omit<Account, 'id' | 'membroNome' | 'ativo'>) => request<Account>('/contas', { method: 'POST', body: JSON.stringify(payload) }),
  updateAccount: (id: string, payload: Omit<Account, 'id' | 'membroNome' | 'ativo'>) => request<Account>(`/contas/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  accountStatus: (id: string, ativo: boolean) => request<Account>(`/contas/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativo) }),
  categories: () => request<Category[]>('/categorias'),
  createCategory: (payload: { nome: string; tipo: CategoryType; categoriaPaiId?: string }) => request<Category>('/categorias', { method: 'POST', body: JSON.stringify(payload) }),
  updateCategory: (id: string, payload: { nome: string; tipo: CategoryType; categoriaPaiId?: string }) => request<Category>(`/categorias/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  categoryStatus: (id: string, ativo: boolean) => request<Category>(`/categorias/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativo) }),
  backup: () => request<{ nomeArquivo: string; criadoEm: string }>('/configuracoes/backup', { method: 'POST' }),
  backupHistory: () => request<Array<{ nomeArquivo: string; criadoEm: string }>>('/configuracoes/backup/historico'),
  restoreBackup: (nomeArquivo: string) => request<object>('/configuracoes/backup/restaurar', { method: 'POST', body: JSON.stringify({ nomeArquivo }) }),
  system: () => request<SystemHealth>('/configuracoes/sistema'),
  settings: () => request<FamilySettings>('/configuracoes/familia'),
  updateSettingsFamily: (payload: { nome: string; moedaPadrao: string; timezone: string }) => request<FamilySettings>('/configuracoes/familia', { method: 'PUT', body: JSON.stringify(payload) }),
  preferences: () => request<UserPreferences>('/configuracoes/preferencias'),
  updatePreferences: (payload: UserPreferences) => request<UserPreferences>('/configuracoes/preferencias', { method: 'PUT', body: JSON.stringify(payload) }),
  notifications: () => request<NotificationSettings>('/configuracoes/notificacoes'),
  updateNotifications: (payload: NotificationSettings) => request<NotificationSettings>('/configuracoes/notificacoes', { method: 'PUT', body: JSON.stringify(payload) }),
  changePassword: (payload: { senhaAtual: string; novaSenha: string }) => request<object>('/seguranca/alterar-senha', { method: 'POST', body: JSON.stringify(payload) }),
  lock: () => request<object>('/seguranca/bloquear', { method: 'POST' }),
  connections: () => request<Array<{ id: string; instituicao: string; tipo: string; status: string; configuracao?: string }>>('/configuracoes/conexoes-financeiras'),
  addConnection: (payload: { instituicao: string; tipo: string; configuracao?: string }) => request<object>('/configuracoes/conexoes-financeiras', { method: 'POST', body: JSON.stringify(payload) }),
  split: (id: string) => request<Split | null>(`/transacoes/${id}/rateio`),
  saveSplit: (id: string, payload: { tipo: Split['tipo']; itens: Array<{ membroId: string; percentual?: number; valorCentavos?: number }>; aplicarComoPadrao?: boolean }) => request<Split>(`/transacoes/${id}/rateio`, { method: 'POST', body: JSON.stringify(payload) }),
  deleteSplit: (id: string) => request<object>(`/transacoes/${id}/rateio`, { method: 'DELETE' }),
  transactions: (query: Record<string, string | number | boolean | undefined> = {}) => { const params = new URLSearchParams(); Object.entries(query).forEach(([key, value]) => value !== undefined && value !== '' && params.set(key, String(value))); return request<TransactionPage>(`/transacoes${params.size ? `?${params}` : ''}`); },
  createTransaction: (payload: TransactionRequest) => request<Transaction>('/transacoes', { method: 'POST', body: JSON.stringify(payload) }),
  updateTransaction: (id: string, payload: TransactionRequest) => request<Transaction>(`/transacoes/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  cancelTransaction: (id: string) => request<Transaction>(`/transacoes/${id}/cancelar`, { method: 'PATCH' }),
  summary: (query: { dataInicio?: string; dataFim?: string } = {}) => api.transactionsSummary(query),
  transactionsSummary: (query: { dataInicio?: string; dataFim?: string } = {}) => { const params = new URLSearchParams(); if (query.dataInicio) params.set('dataInicio', query.dataInicio); if (query.dataFim) params.set('dataFim', query.dataFim); return request<FinanceSummary>(`/movimentacoes/resumo${params.size ? `?${params}` : ''}`); },
  accountBalance: (id: string) => request<AccountBalance>(`/contas/${id}/saldo`),
  ledger: (query: Record<string, string | number | boolean | undefined> = {}) => request<LedgerPage>(`/lancamentos${queryString(query)}`),
  createLedger: (payload: LedgerRequest) => request<Ledger>('/lancamentos', { method: 'POST', body: JSON.stringify(payload) }),
  updateLedger: (id: string, payload: LedgerRequest) => request<Ledger>(`/lancamentos/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  effectLedger: (id: string, payload: { valorCentavos?: number; dataEfetivacao?: string } = {}) => request<Ledger>(`/lancamentos/${id}/efetivar`, { method: 'POST', body: JSON.stringify(payload) }),
  cancelLedger: (id: string) => request<Ledger>(`/lancamentos/${id}/cancelar`, { method: 'POST' }),
  accountLedgerProjection: (id: string, data?: string) => request<LedgerBalance>(`/contas/${id}/projecao${data ? `?data=${encodeURIComponent(data)}` : ''}`),
  accountStatement: (id: string, query: Record<string, string | undefined> = {}) => { const params = new URLSearchParams(); Object.entries(query).forEach(([key, value]) => value && params.set(key, value)); return request<AccountStatement>(`/contas/${id}/extrato${params.size ? `?${params}` : ''}`); },
  transfers: () => request<Transfer[]>('/transferencias'),
  createTransfer: (payload: TransferRequest) => request<Transfer>('/transferencias', { method: 'POST', body: JSON.stringify(payload) }),
  updateTransfer: (id: string, payload: TransferRequest) => request<Transfer>(`/transferencias/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  cancelTransfer: (id: string) => request<Transfer>(`/transferencias/${id}/cancelar`, { method: 'PATCH' }),
  cards: () => request<Card[]>('/cartoes'),
  createCard: (payload: CardRequest) => request<Card>('/cartoes', { method: 'POST', body: JSON.stringify(payload) }),
  updateCard: (id: string, payload: CardRequest) => request<Card>(`/cartoes/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  cardStatus: (id: string, ativo: boolean) => request<Card>(`/cartoes/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativo) }),
  cardLimit: (id: string) => request<CardLimit>(`/cartoes/${id}/limite`),
  cardPurchases: (cardId?: string) => request<CardPurchase[]>(`/compras-cartao${cardId ? `?cartaoId=${cardId}` : ''}`),
  createCardPurchase: (payload: CardPurchaseRequest) => request<CardPurchase>('/compras-cartao', { method: 'POST', body: JSON.stringify(payload) }),
  cancelCardPurchase: (id: string) => request<CardPurchase>(`/compras-cartao/${id}/cancelar`, { method: 'PATCH' }),
  refundCardPurchase: (id: string, payload: { valorCentavos: number; data: string; descricao: string; faturaId?: string }) => request<CardPurchase>(`/compras-cartao/${id}/estornar`, { method: 'POST', body: JSON.stringify(payload) }),
  invoices: (cardId?: string, status?: InvoiceStatus) => { const params = new URLSearchParams(); if (cardId) params.set('cartaoId', cardId); if (status) params.set('status', status); return request<Invoice[]>(`/faturas${params.size ? `?${params}` : ''}`); },
  invoice: (id: string) => request<InvoiceDetail>(`/faturas/${id}`),
  closeInvoice: (id: string) => request<Invoice>(`/faturas/${id}/fechar`, { method: 'PATCH' }),
  payInvoice: (id: string, payload: { contaPagamentoId: string; dataPagamento: string; valorCentavos: number }) => request<Invoice>(`/faturas/${id}/pagar`, { method: 'POST', body: JSON.stringify(payload) }),
  cardProjection: (id: string) => request<InvoiceProjection[]>(`/cartoes/${id}/projecao`),
  recurrences: (tipo?: TransactionType) => request<Recurrence[]>(`/recorrencias${tipo ? `?tipo=${tipo}` : ''}`),
  recurringIncomes: () => request<Recurrence[]>('/recorrencias/receitas'),
  recurringExpenses: () => request<Recurrence[]>('/recorrencias/despesas'),
  createRecurrence: (payload: RecurrenceRequest) => request<Recurrence>('/recorrencias', { method: 'POST', body: JSON.stringify(payload) }),
  updateRecurrence: (id: string, payload: RecurrenceRequest) => request<Recurrence>(`/recorrencias/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  pauseRecurrence: (id: string) => request<Recurrence>(`/recorrencias/${id}/pausar`, { method: 'PATCH' }),
  reactivateRecurrence: (id: string) => request<Recurrence>(`/recorrencias/${id}/reativar`, { method: 'PATCH' }),
  closeRecurrence: (id: string, dataFim?: string) => request<Recurrence>(`/recorrencias/${id}/encerrar`, { method: 'PATCH', body: JSON.stringify(dataFim ?? null) }),
  recurrenceSummary: () => request<RecurrenceSummary>('/recorrencias/resumo'),
  processRecurrences: () => request<{ processadoEm: string }>('/recorrencias/processar', { method: 'POST' }),
  upcomingRecurrences: (dias = 30) => request<RecurrenceOccurrence[]>(`/recorrencias/proximas?dias=${dias}`),
  recurrenceOccurrences: (query: Record<string, string | undefined> = {}) => { const params = new URLSearchParams(); Object.entries(query).forEach(([key, value]) => value && params.set(key, value)); return request<RecurrenceOccurrence[]>(`/ocorrencias-recorrentes${params.size ? `?${params}` : ''}`); },
  effectOccurrence: (id: string, payload: { valorRealizadoCentavos: number; dataEfetivacao?: string; observacao?: string }) => request<RecurrenceOccurrence>(`/ocorrencias-recorrentes/${id}/efetivar`, { method: 'POST', body: JSON.stringify(payload) }),
  ignoreOccurrence: (id: string) => request<RecurrenceOccurrence>(`/ocorrencias-recorrentes/${id}/ignorar`, { method: 'PATCH' }),
  updateOccurrence: (id: string, payload: { valorPrevistoCentavos: number; dataPrevista?: string; observacao?: string }) => request<RecurrenceOccurrence>(`/ocorrencias-recorrentes/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  subscriptions: () => request<Subscription[]>('/assinaturas'),
  subscriptionSummary: () => request<SubscriptionSummary>('/assinaturas/resumo'),
  createSubscription: (payload: SubscriptionRequest) => request<Subscription>('/assinaturas', { method: 'POST', body: JSON.stringify(payload) }),
  updateSubscription: (id: string, payload: SubscriptionRequest) => request<Subscription>(`/assinaturas/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  subscriptionStatus: (id: string, ativa: boolean) => request<Subscription>(`/assinaturas/${id}/status`, { method: 'PATCH', body: JSON.stringify(ativa) }),
  dashboardSummary: (query: DashboardQuery = {}) => { const params = new URLSearchParams(); Object.entries(query).forEach(([key, value]) => value !== undefined && value !== '' && params.set(key, String(value))); return request<DashboardSummary>(`/dashboard/resumo${params.size ? `?${params}` : ''}`); },
  dashboardBalance: (query: DashboardQuery = {}) => request<DashboardBalance>(`/dashboard/saldo${queryString(query)}`),
  dashboardAccounts: (query: DashboardQuery = {}) => request<DashboardAccount[]>(`/dashboard/saldos-contas${queryString(query)}`),
  dashboardCashFlow: (query: DashboardQuery = {}) => request<DashboardCashFlow>(`/dashboard/fluxo-caixa${queryString(query)}`),
  dashboardProjection: (query: DashboardQuery = {}) => request<DashboardProjection>(`/dashboard/saldo-projetado${queryString(query)}`),
  dashboardKpis: (query: DashboardQuery = {}) => request<DashboardKpis>(`/dashboard/kpis${queryString(query)}`),
  dashboardBudget: (query: DashboardQuery = {}) => request<BudgetDashboard | null>(`/dashboard/orcamento${queryString(query)}`),
  alerts: (query: { tipo?: AlertType; severidade?: AlertSeverity; resolvido?: boolean } = {}) => request<FinancialAlert[]>(`/alertas${queryString(query)}`),
  markAlertRead: (id: string) => request<FinancialAlert>(`/alertas/${id}/lido`, { method: 'POST' }),
  resolveAlert: (id: string) => request<FinancialAlert>(`/alertas/${id}/resolver`, { method: 'POST' }),
  budgets: (query: { ano?: number; mes?: number; membroId?: string; status?: BudgetStatus } = {}) => request<Budget[]>(`/orcamentos${queryString(query)}`),
  budget: (id: string) => request<Budget>(`/orcamentos/${id}`),
  createBudget: (payload: BudgetRequest) => request<Budget>('/orcamentos', { method: 'POST', body: JSON.stringify(payload) }),
  updateBudget: (id: string, payload: BudgetRequest) => request<Budget>(`/orcamentos/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  activateBudget: (id: string) => request<Budget>(`/orcamentos/${id}/ativar`, { method: 'POST' }),
  closeBudget: (id: string) => request<Budget>(`/orcamentos/${id}/encerrar`, { method: 'POST' }),
  reopenBudget: (id: string) => request<Budget>(`/orcamentos/${id}/reabrir`, { method: 'POST' }),
  budgetItems: (id: string) => request<BudgetItem[]>(`/orcamentos/${id}/itens`),
  addBudgetItem: (id: string, payload: BudgetItemRequest) => request<BudgetItem>(`/orcamentos/${id}/itens`, { method: 'POST', body: JSON.stringify(payload) }),
  updateBudgetItem: (id: string, itemId: string, payload: BudgetItemRequest) => request<BudgetItem>(`/orcamentos/${id}/itens/${itemId}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteBudgetItem: (id: string, itemId: string) => request<object>(`/orcamentos/${id}/itens/${itemId}`, { method: 'DELETE' }),
  copyBudget: (id: string, payload: { ano: number; mes: number; membroId?: string; descricao?: string; reajustePercentual?: number }) => request<Budget>(`/orcamentos/${id}/copiar`, { method: 'POST', body: JSON.stringify(payload) }),
  budgetRealized: (id: string) => request<BudgetSummary>(`/orcamentos/${id}/realizado`),
  budgetComparison: (id: string) => request<BudgetComparison>(`/orcamentos/${id}/comparativo`),
  budgetProjection: (id: string) => request<BudgetProjection>(`/orcamentos/${id}/projecao`),
  budgetSummary: (id: string) => request<BudgetSummary>(`/orcamentos/${id}/resumo`),
};

function queryString(query: Record<string, string | number | boolean | undefined>) { const params = new URLSearchParams(); Object.entries(query).forEach(([key, value]) => value !== undefined && value !== '' && params.set(key, String(value))); return params.size ? `?${params}` : ''; }
