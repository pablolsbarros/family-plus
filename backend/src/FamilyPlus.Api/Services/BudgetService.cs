using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class BudgetService(FinanceDbContext db, AuditService audit, RecurrenceService recurrences, Func<DateTimeOffset>? utcNow = null)
{
    public async Task<IReadOnlyList<BudgetResponse>> ListAsync(int? year = null, int? month = null, Guid? memberId = null, StatusOrcamento? status = null)
    {
        var query = db.Orcamentos.AsNoTracking().Include(x => x.Membro).AsQueryable();
        if (year.HasValue) query = query.Where(x => x.Ano == year.Value);
        if (month.HasValue) query = query.Where(x => x.Mes == month.Value);
        if (memberId.HasValue) query = query.Where(x => x.MembroId == memberId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return (await query.ToListAsync()).OrderByDescending(x => x.Ano).ThenByDescending(x => x.Mes).ThenBy(x => x.Descricao).Select(Map).ToList();
    }

    public async Task<BudgetResponse> GetAsync(Guid id) => Map(await FindAsync(id));

    public async Task<BudgetResponse> CreateAsync(BudgetRequest request, Guid? userId)
    {
        ValidateHeader(request);
        if (await db.Orcamentos.AnyAsync(x => x.Ano == request.Ano && x.Mes == request.Mes && x.MembroId == request.MembroId))
            throw new DomainException("Já existe um orçamento para este mês e membro.");
        if (request.MembroId.HasValue && !await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo))
            throw new DomainException("O membro informado não existe ou está desativado.");
        var budget = new Orcamento { Ano = request.Ano, Mes = request.Mes, MembroId = request.MembroId, Descricao = request.Descricao.Trim(), ReceitaPrevistaCentavos = request.ReceitaPrevistaCentavos, ModoReceitaPrevista = request.ModoReceitaPrevista, Status = request.Status, Observacao = Trim(request.Observacao) };
        db.Orcamentos.Add(budget);
        await audit.RecordAsync("ORCAMENTO", budget.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(budget), userId);
        await db.SaveChangesAsync();
        return await GetAsync(budget.Id);
    }

    public async Task<BudgetResponse> UpdateAsync(Guid id, BudgetRequest request, Guid? userId)
    {
        ValidateHeader(request);
        var budget = await FindAsync(id);
        EnsureEditable(budget);
        if (await db.Orcamentos.AnyAsync(x => x.Id != id && x.Ano == request.Ano && x.Mes == request.Mes && x.MembroId == request.MembroId))
            throw new DomainException("Já existe um orçamento para este mês e membro.");
        if (request.MembroId.HasValue && !await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo))
            throw new DomainException("O membro informado não existe ou está desativado.");
        var before = Snapshot(budget);
        budget.Ano = request.Ano; budget.Mes = request.Mes; budget.MembroId = request.MembroId; budget.Descricao = request.Descricao.Trim(); budget.ReceitaPrevistaCentavos = request.ReceitaPrevistaCentavos; budget.ModoReceitaPrevista = request.ModoReceitaPrevista; budget.Status = request.Status; budget.Observacao = Trim(request.Observacao);
        await audit.RecordAsync("ORCAMENTO", budget.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(budget), userId);
        await db.SaveChangesAsync();
        return await GetAsync(id);
    }

    public Task<BudgetResponse> ActivateAsync(Guid id, Guid? userId) => SetStatusAsync(id, StatusOrcamento.ATIVO, userId);

    public Task<BudgetResponse> CloseAsync(Guid id, Guid? userId) => SetStatusAsync(id, StatusOrcamento.ENCERRADO, userId);

    public Task<BudgetResponse> ReopenAsync(Guid id, Guid? userId) => SetStatusAsync(id, StatusOrcamento.ATIVO, userId);

    public async Task<IReadOnlyList<BudgetItemResponse>> ListItemsAsync(Guid budgetId)
    {
        var budget = await FindAsync(budgetId);
        return budget.Itens.OrderBy(x => x.Categoria?.Nome).ThenBy(x => x.Membro?.Nome).Select(Map).ToList();
    }

    public async Task<BudgetItemResponse> AddItemAsync(Guid budgetId, BudgetItemRequest request, Guid? userId)
    {
        var budget = await FindAsync(budgetId);
        EnsureEditable(budget);
        await ValidateItemAsync(budget, request, null);
        var item = new ItemOrcamento { OrcamentoId = budgetId, CategoriaId = request.CategoriaId, MembroId = request.MembroId, ValorPlanejadoCentavos = request.ValorPlanejadoCentavos, Observacao = Trim(request.Observacao) };
        db.ItensOrcamento.Add(item);
        await audit.RecordAsync("ITEM_ORCAMENTO", item.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(item), userId);
        await db.SaveChangesAsync();
        return Map(await FindItemAsync(item.Id));
    }

    public async Task<BudgetItemResponse> UpdateItemAsync(Guid budgetId, Guid itemId, BudgetItemRequest request, Guid? userId)
    {
        var budget = await FindAsync(budgetId);
        EnsureEditable(budget);
        var item = await FindItemAsync(itemId);
        if (item.OrcamentoId != budgetId) throw new DomainException("O item não pertence ao orçamento informado.");
        await ValidateItemAsync(budget, request, itemId);
        var before = Snapshot(item);
        item.CategoriaId = request.CategoriaId; item.MembroId = request.MembroId; item.ValorPlanejadoCentavos = request.ValorPlanejadoCentavos; item.Observacao = Trim(request.Observacao);
        await audit.RecordAsync("ITEM_ORCAMENTO", item.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(item), userId);
        await db.SaveChangesAsync();
        return Map(await FindItemAsync(itemId));
    }

    public async Task DeleteItemAsync(Guid budgetId, Guid itemId, Guid? userId)
    {
        var budget = await FindAsync(budgetId);
        EnsureEditable(budget);
        var item = await FindItemAsync(itemId);
        if (item.OrcamentoId != budgetId) throw new DomainException("O item não pertence ao orçamento informado.");
        var before = Snapshot(item);
        item.Ativo = false;
        await audit.RecordAsync("ITEM_ORCAMENTO", item.Id, OperacaoAuditoria.CANCELAMENTO, before, Snapshot(item), userId);
        await db.SaveChangesAsync();
    }

    public async Task<BudgetResponse> CopyAsync(Guid sourceId, BudgetCopyRequest request, Guid? userId)
    {
        ValidatePeriod(request.Ano, request.Mes);
        var source = await FindAsync(sourceId);
        if (await db.Orcamentos.AnyAsync(x => x.Ano == request.Ano && x.Mes == request.Mes && x.MembroId == request.MembroId))
            throw new DomainException("Já existe um orçamento para o mês de destino.");
        var target = new Orcamento { Ano = request.Ano, Mes = request.Mes, MembroId = request.MembroId, Descricao = string.IsNullOrWhiteSpace(request.Descricao) ? $"Orçamento {request.Mes:00}/{request.Ano}" : request.Descricao.Trim(), ReceitaPrevistaCentavos = Adjust(source.ReceitaPrevistaCentavos, request.ReajustePercentual), ModoReceitaPrevista = source.ModoReceitaPrevista, Status = StatusOrcamento.RASCUNHO, Observacao = source.Observacao };
        db.Orcamentos.Add(target);
        await db.SaveChangesAsync();
        var sourceItems = source.Itens.Where(x => x.Ativo).ToList();
        foreach (var item in sourceItems)
        {
            db.ItensOrcamento.Add(new ItemOrcamento { OrcamentoId = target.Id, CategoriaId = item.CategoriaId, MembroId = item.MembroId, ValorPlanejadoCentavos = Adjust(item.ValorPlanejadoCentavos, request.ReajustePercentual), Observacao = item.Observacao });
        }
        await audit.RecordAsync("ORCAMENTO", target.Id, OperacaoAuditoria.CRIACAO, null, new { CopiadoDe = source.Id, target.Ano, target.Mes, request.ReajustePercentual }, userId);
        await db.SaveChangesAsync();
        return await GetAsync(target.Id);
    }

    public async Task<BudgetSummaryResponse> SummaryAsync(Guid id)
    {
        var budget = await FindAsync(id);
        var calculation = await CalculateAsync(budget);
        return new BudgetSummaryResponse(Map(budget), calculation.TotalPlanned, calculation.TotalRealized, calculation.TotalPlanned - calculation.TotalRealized, Percentage(calculation.TotalRealized, calculation.TotalPlanned), calculation.TotalProjected, Percentage(calculation.TotalProjected, calculation.TotalPlanned), budget.ReceitaPrevistaCentavos - calculation.TotalPlanned, Percentage(budget.ReceitaPrevistaCentavos - calculation.TotalPlanned, budget.ReceitaPrevistaCentavos), calculation.Lines);
    }

    public async Task<BudgetComparisonResponse> ComparisonAsync(Guid id)
    {
        var summary = await SummaryAsync(id);
        return new BudgetComparisonResponse(summary.Orcamento, summary.Linhas, summary.TotalPlanejadoCentavos, summary.TotalRealizadoCentavos, summary.TotalRealizadoCentavos - summary.TotalPlanejadoCentavos, Percentage(summary.TotalRealizadoCentavos - summary.TotalPlanejadoCentavos, summary.TotalPlanejadoCentavos));
    }

    public async Task<BudgetProjectionResponse> ProjectionAsync(Guid id)
    {
        var summary = await SummaryAsync(id);
        return new BudgetProjectionResponse(summary.Orcamento, summary.Linhas, summary.TotalPlanejadoCentavos, summary.TotalRealizadoCentavos, summary.TotalProjetadoCentavos, Math.Max(0, summary.TotalProjetadoCentavos - summary.TotalPlanejadoCentavos));
    }

    public async Task<BudgetDashboardResponse?> DashboardAsync(int year, int month, Guid? memberId = null)
    {
        var budget = await db.Orcamentos.Include(x => x.Membro).Include(x => x.Itens).ThenInclude(x => x.Categoria).Include(x => x.Itens).ThenInclude(x => x.Membro).Where(x => x.Ano == year && x.Mes == month && x.MembroId == memberId && x.Status != StatusOrcamento.RASCUNHO).OrderByDescending(x => x.Status == StatusOrcamento.ATIVO).FirstOrDefaultAsync();
        if (budget is null) return null;
        var summary = await SummaryAsync(budget.Id);
        return new BudgetDashboardResponse(budget.Id, year, month, budget.ReceitaPrevistaCentavos, summary.TotalPlanejadoCentavos, summary.TotalRealizadoCentavos, summary.SaldoOrcamentarioCentavos, summary.PercentualUtilizado, summary.Linhas.Where(x => !x.SemOrcamento).OrderByDescending(x => x.PercentualUtilizado).Take(5).ToList());
    }

    public async Task<IReadOnlySet<string>> RefreshAlertsAsync()
    {
        await recurrences.GenerateAllAsync(UtcNow());
        var budgets = await db.Orcamentos.Where(x => x.Status == StatusOrcamento.ATIVO).ToListAsync();
        var keys = new HashSet<string>();
        foreach (var budget in budgets)
        {
            var summary = await SummaryAsync(budget.Id);
            foreach (var line in summary.Linhas)
            {
                if (line.SemOrcamento && line.RealizadoCentavos > 0)
                {
                    var missingKey = $"CATEGORIA_SEM_ORCAMENTO:{budget.Id}:{line.CategoriaId}:{line.MembroId}"; keys.Add(missingKey);
                    await EnsureAlertAsync(missingKey, TipoAlerta.CATEGORIA_SEM_ORCAMENTO, SeveridadeAlerta.ATENCAO, "Categoria sem orçamento", $"{line.CategoriaNome} possui {FormatMoney(line.RealizadoCentavos)} realizado no mês, mas ainda não tem limite planejado.", budget.Id);
                    continue;
                }
                if (line.ItemId is null || line.ValorPlanejadoCentavos <= 0) continue;
                TipoAlerta? type = line.PercentualProjetado >= 100 && line.PercentualUtilizado < 100 ? TipoAlerta.ORCAMENTO_ESTOURO_PROJETADO : line.PercentualUtilizado >= 100 ? TipoAlerta.ORCAMENTO_ESTOURADO : line.PercentualUtilizado >= 90 ? TipoAlerta.ORCAMENTO_CRITICO : line.PercentualUtilizado >= 70 ? TipoAlerta.ORCAMENTO_ATENCAO : null;
                if (!type.HasValue) continue;
                var severity = type is TipoAlerta.ORCAMENTO_ESTOURADO or TipoAlerta.ORCAMENTO_CRITICO ? SeveridadeAlerta.CRITICO : SeveridadeAlerta.ATENCAO;
                var alertKey = $"{type}:{budget.Id}:{line.ItemId}"; keys.Add(alertKey);
                var title = type switch { TipoAlerta.ORCAMENTO_ESTOURADO => "Orçamento estourado", TipoAlerta.ORCAMENTO_ESTOURO_PROJETADO => "Estouro de orçamento projetado", TipoAlerta.ORCAMENTO_CRITICO => "Orçamento em nível crítico", _ => "Atenção ao orçamento" };
                var message = type == TipoAlerta.ORCAMENTO_ESTOURADO ? $"O orçamento de {line.CategoriaNome} foi ultrapassado em {FormatMoney(Math.Abs(line.DisponivelCentavos))}. Planejado: {FormatMoney(line.ValorPlanejadoCentavos)}; realizado: {FormatMoney(line.RealizadoCentavos)}." : type == TipoAlerta.ORCAMENTO_ESTOURO_PROJETADO ? $"O orçamento de {line.CategoriaNome} ainda não foi ultrapassado, mas os gastos previstos indicam {FormatMoney(line.ProjetadoCentavos)} até o fim do mês." : $"Você já utilizou {line.PercentualUtilizado:0.##}% do orçamento de {line.CategoriaNome}. Planejado: {FormatMoney(line.ValorPlanejadoCentavos)}; realizado: {FormatMoney(line.RealizadoCentavos)}.";
                await EnsureAlertAsync(alertKey, type.Value, severity, title, message, budget.Id);
            }
        }
        return keys;
    }

    private async Task<BudgetCalculation> CalculateAsync(Orcamento budget)
    {
        var categories = await db.Categorias.AsNoTracking().Where(x => x.Ativo).ToListAsync();
        var members = await db.Membros.AsNoTracking().Where(x => x.Ativo).ToDictionaryAsync(x => x.Id, x => x.Nome);
        var transactions = await db.Transacoes.AsNoTracking().Where(x => x.Status == StatusTransacao.EFETIVADA && x.Tipo == TipoTransacao.DESPESA && x.Origem != OrigemTransacao.PAGAMENTO_FATURA).ToListAsync();
        var predictedTransactions = await db.Transacoes.AsNoTracking().Where(x => x.Status == StatusTransacao.PREVISTA && x.Tipo == TipoTransacao.DESPESA && x.Origem != OrigemTransacao.PAGAMENTO_FATURA).ToListAsync();
        var purchases = await db.ComprasCartao.AsNoTracking().Where(x => x.Status != StatusCompraCartao.CANCELADA).ToListAsync();
        var refunds = (await db.EstornosCartao.AsNoTracking().ToListAsync()).GroupBy(x => x.CompraCartaoId).ToDictionary(x => x.Key, x => x.Sum(y => y.ValorCentavos));
        var occurrences = await db.OcorrenciasRecorrentes.AsNoTracking().Include(x => x.Recorrencia).Where(x => x.Status == StatusOcorrenciaRecorrencia.PENDENTE || x.Status == StatusOcorrenciaRecorrencia.ATRASADA).ToListAsync();
        var monthStart = new DateTimeOffset(budget.Ano, budget.Mes, 1, 12, 0, 0, TimeSpan.Zero);
        var monthEnd = monthStart.AddMonths(1);
        var today = Normalize(UtcNow());
        var items = budget.Itens.Where(x => x.Ativo).ToList();
        var itemValues = items.ToDictionary(x => x.Id, _ => new ValuePair());
        var missing = new Dictionary<(Guid CategoryId, Guid? MemberId), ValuePair>();

        var actualMovements = new List<BudgetMovement>();
        actualMovements.AddRange(transactions.Where(x => InMonth(x.DataCompetencia, monthStart, monthEnd)).Select(x => new BudgetMovement(x.CategoriaId ?? Guid.Empty, x.MembroId, x.ValorCentavos, false)));
        actualMovements.AddRange(purchases.Where(x => InMonth(x.DataCompra, monthStart, monthEnd)).Select(x => new BudgetMovement(x.CategoriaId, x.MembroId, Math.Max(0, x.ValorTotalCentavos - refunds.GetValueOrDefault(x.Id)), false)));
        foreach (var movement in actualMovements) Allocate(movement, items, categories, budget.MembroId, itemValues, missing, false);

        var projectedMovements = predictedTransactions.Where(x => x.DataCompetencia >= today && InMonth(x.DataCompetencia, monthStart, monthEnd)).Select(x => new BudgetMovement(x.CategoriaId ?? Guid.Empty, x.MembroId, x.ValorCentavos, true)).ToList();
        projectedMovements.AddRange(occurrences.Where(x => x.Recorrencia is not null && x.Recorrencia.Tipo == TipoTransacao.DESPESA && x.DataPrevista >= today && InMonth(x.DataPrevista, monthStart, monthEnd)).Select(x => new BudgetMovement(x.Recorrencia!.CategoriaId, x.Recorrencia.MembroId, x.ValorPrevistoCentavos, true)));
        foreach (var movement in projectedMovements) Allocate(movement, items, categories, budget.MembroId, itemValues, missing, true);

        var lines = new List<BudgetLineResponse>();
        foreach (var item in items)
        {
            var value = itemValues[item.Id];
            lines.Add(BuildLine(item, item.CategoriaId, value.Actual, value.Projected, item.Categoria?.Nome ?? CategoryName(item.CategoriaId, categories), item.Membro?.Nome));
        }
        foreach (var pair in missing.OrderBy(x => CategoryName(x.Key.CategoryId, categories)))
        {
            var value = pair.Value;
            lines.Add(BuildLine(null, pair.Key.CategoryId, value.Actual, value.Projected, CategoryName(pair.Key.CategoryId, categories), MemberName(pair.Key.MemberId, members)));
        }
        return new BudgetCalculation(lines, lines.Sum(x => x.ValorPlanejadoCentavos), lines.Sum(x => x.RealizadoCentavos), lines.Sum(x => x.ProjetadoCentavos));
    }

    private static void Allocate(BudgetMovement movement, IReadOnlyList<ItemOrcamento> items, IReadOnlyList<Categoria> categories, Guid? budgetMemberId, IDictionary<Guid, ValuePair> itemValues, IDictionary<(Guid CategoryId, Guid? MemberId), ValuePair> missing, bool projected)
    {
        if (budgetMemberId.HasValue && movement.MemberId != budgetMemberId) return;
        var candidates = items.Where(item => MemberMatches(item.MembroId, movement.MemberId) && IsInBranch(movement.CategoryId, item.CategoriaId, categories)).OrderByDescending(item => item.MembroId.HasValue).ThenByDescending(item => item.CategoriaId == movement.CategoryId).ToList();
        var target = candidates.FirstOrDefault();
        if (target is not null)
        {
            var value = itemValues[target.Id];
            if (projected) value.Projected += movement.Value; else value.Actual += movement.Value;
            return;
        }
        var key = (movement.CategoryId, movement.MemberId);
        if (!missing.TryGetValue(key, out var unbudgeted)) missing[key] = unbudgeted = new ValuePair();
        if (projected) unbudgeted.Projected += movement.Value; else unbudgeted.Actual += movement.Value;
    }

    private static BudgetLineResponse BuildLine(ItemOrcamento? item, Guid categoryId, long actual, long projected, string categoryName, string? memberName)
    {
        var planned = item?.ValorPlanejadoCentavos ?? 0;
        var available = planned - actual;
        var usage = Percentage(actual, planned);
        var projectedPercentage = Percentage(projected + actual, planned);
        var situation = item is null ? "SEM_ORCAMENTO" : usage >= 100 ? "ESTOURADO" : usage >= 90 ? "CRITICO" : usage >= 70 ? "ATENCAO" : "SAUDAVEL";
        return new BudgetLineResponse(item?.Id, categoryId, categoryName, item?.MembroId, memberName, planned, actual, available, usage, actual - planned, Percentage(actual - planned, planned), actual + projected, projectedPercentage, situation, item is null);
    }

    private async Task ValidateItemAsync(Orcamento budget, BudgetItemRequest request, Guid? editingId)
    {
        if (request.CategoriaId == Guid.Empty || request.ValorPlanejadoCentavos <= 0) throw new DomainException("Informe uma categoria de despesa e um valor planejado positivo.");
        var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaId);
        if (category is null || !category.Ativo || category.Tipo != TipoCategoria.Despesa) throw new DomainException("A categoria informada não existe, está inativa ou não é de despesa.");
        if (request.MembroId.HasValue && !await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("O membro do item não existe ou está desativado.");
        if (await db.ItensOrcamento.AnyAsync(x => x.OrcamentoId == budget.Id && x.Id != editingId && x.CategoriaId == request.CategoriaId && x.MembroId == request.MembroId && x.Ativo)) throw new DomainException("Esta categoria e membro já possuem um item neste orçamento.");
        var otherItems = await db.ItensOrcamento.Where(x => x.OrcamentoId == budget.Id && x.Id != editingId && x.Ativo).ToListAsync();
        var categories = await db.Categorias.AsNoTracking().Where(x => x.Ativo).ToListAsync();
        if (otherItems.Any(item => MemberScopesOverlap(item.MembroId, request.MembroId) && (IsAncestor(item.CategoriaId, request.CategoriaId, categories) || IsAncestor(request.CategoriaId, item.CategoriaId, categories))))
            throw new DomainException("Não é permitido planejar uma categoria pai e uma filha no mesmo ramo; escolha o pai ou as subcategorias para evitar dupla contagem.");
    }

    private async Task<BudgetResponse> SetStatusAsync(Guid id, StatusOrcamento status, Guid? userId)
    {
        var budget = await FindAsync(id);
        if (status == StatusOrcamento.ATIVO && budget.Itens.All(x => !x.Ativo)) throw new DomainException("Adicione ao menos um item antes de ativar o orçamento.");
        var before = Snapshot(budget); budget.Status = status;
        await audit.RecordAsync("ORCAMENTO", budget.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(budget), userId);
        await db.SaveChangesAsync();
        return Map(budget);
    }

    private async Task<Orcamento> FindAsync(Guid id) => await db.Orcamentos.Include(x => x.Membro).Include(x => x.Itens.Where(i => i.Ativo)).ThenInclude(x => x.Categoria).Include(x => x.Itens.Where(i => i.Ativo)).ThenInclude(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Orçamento não encontrado.");
    private async Task<ItemOrcamento> FindItemAsync(Guid id) => await db.ItensOrcamento.Include(x => x.Categoria).Include(x => x.Membro).SingleOrDefaultAsync(x => x.Id == id && x.Ativo) ?? throw new DomainException("Item de orçamento não encontrado.");
    private static void EnsureEditable(Orcamento budget) { if (budget.Status == StatusOrcamento.ENCERRADO) throw new DomainException("Orçamento encerrado não pode ser alterado. Reabra-o para editar."); }
    private static void ValidateHeader(BudgetRequest request) { ValidatePeriod(request.Ano, request.Mes); if (string.IsNullOrWhiteSpace(request.Descricao)) throw new DomainException("A descrição do orçamento é obrigatória."); if (request.ReceitaPrevistaCentavos < 0) throw new DomainException("A receita prevista não pode ser negativa."); }
    private static void ValidatePeriod(int year, int month) { if (year < 2000 || year > 2200 || month is < 1 or > 12) throw new DomainException("Informe um ano e mês válidos para o orçamento."); }
    private static BudgetResponse Map(Orcamento x) => new(x.Id, x.Ano, x.Mes, x.MembroId, x.Membro?.Nome, x.Descricao, x.ReceitaPrevistaCentavos, x.ModoReceitaPrevista, x.Status, x.Observacao, x.CriadoEm, x.AtualizadoEm);
    private static BudgetItemResponse Map(ItemOrcamento x) => new(x.Id, x.OrcamentoId, x.CategoriaId, x.Categoria?.Nome ?? "", x.MembroId, x.Membro?.Nome, x.ValorPlanejadoCentavos, x.Observacao);
    private static object Snapshot(Orcamento x) => new { x.Id, x.Ano, x.Mes, x.MembroId, x.Descricao, x.ReceitaPrevistaCentavos, x.Status };
    private static object Snapshot(ItemOrcamento x) => new { x.Id, x.OrcamentoId, x.CategoriaId, x.MembroId, x.ValorPlanejadoCentavos };
    private async Task EnsureAlertAsync(string key, TipoAlerta type, SeveridadeAlerta severity, string title, string message, Guid budgetId)
    {
        var alert = await db.Alertas.SingleOrDefaultAsync(x => x.ChaveUnica == key);
        if (alert is null) db.Alertas.Add(new Alerta { ChaveUnica = key, Tipo = type, Severidade = severity, Titulo = title, Mensagem = message, EntidadeOrigem = "ORCAMENTO", EntidadeId = budgetId });
        else if (!alert.Resolvido) { alert.Severidade = severity; alert.Titulo = title; alert.Mensagem = message; alert.DataGeracao = DateTimeOffset.UtcNow; }
        await db.SaveChangesAsync();
    }
    private static long Adjust(long value, decimal percent) => checked((long)Math.Round(value * (1 + percent / 100m), MidpointRounding.AwayFromZero));
    private static decimal Percentage(long numerator, long denominator) => denominator == 0 ? 0 : Math.Round((decimal)numerator * 100 / denominator, 2);
    private DateTimeOffset UtcNow() => utcNow?.Invoke() ?? DateTimeOffset.UtcNow;
    private static bool InMonth(DateTimeOffset value, DateTimeOffset start, DateTimeOffset end) => value.Date >= start.Date && value.Date < end.Date;
    private static DateTimeOffset Normalize(DateTimeOffset value) => new(value.Year, value.Month, value.Day, 12, 0, 0, TimeSpan.Zero);
    private static bool MemberMatches(Guid? itemMemberId, Guid movementMemberId) => !itemMemberId.HasValue || itemMemberId.Value == movementMemberId;
    private static bool MemberScopesOverlap(Guid? first, Guid? second) => !first.HasValue || !second.HasValue || first == second;
    private static bool IsInBranch(Guid categoryId, Guid budgetCategoryId, IReadOnlyList<Categoria> categories) => categoryId == budgetCategoryId || IsAncestor(budgetCategoryId, categoryId, categories);
    private static bool IsAncestor(Guid ancestorId, Guid childId, IReadOnlyList<Categoria> categories)
    {
        var cursor = categories.FirstOrDefault(x => x.Id == childId);
        var guard = 0;
        while (cursor?.CategoriaPaiId is Guid parentId && guard++ < categories.Count) { if (parentId == ancestorId) return true; cursor = categories.FirstOrDefault(x => x.Id == parentId); }
        return false;
    }
    private static string CategoryName(Guid id, IReadOnlyList<Categoria> categories) => categories.FirstOrDefault(x => x.Id == id)?.Nome ?? "Sem categoria";
    private static string? MemberName(Guid? id, IReadOnlyDictionary<Guid, string> members) => id.HasValue ? members.GetValueOrDefault(id.Value) : null;
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string FormatMoney(long cents) => $"R$ {(cents / 100m):N2}";
    private sealed record BudgetMovement(Guid CategoryId, Guid MemberId, long Value, bool Projected);
    private sealed class ValuePair { public long Actual; public long Projected; }
    private sealed record BudgetCalculation(IReadOnlyList<BudgetLineResponse> Lines, long TotalPlanned, long TotalRealized, long TotalProjected);
}
