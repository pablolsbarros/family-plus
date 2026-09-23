using FamilyPlus.Api.Data;
using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyPlus.Api.Services;

public sealed class RecurrenceService(FinanceDbContext db, AuditService audit, TransactionService transactions, CardService cards)
{
    private const int DefaultHorizonMonths = 12;

    public async Task<IReadOnlyList<RecurrenceResponse>> ListAsync(TipoTransacao? type = null)
    {
        await GenerateAllAsync(DateTimeOffset.UtcNow);
        var query = db.Recorrencias.AsNoTracking().Include(x => x.Membro).Include(x => x.Conta).Include(x => x.Categoria).AsQueryable();
        if (type.HasValue) query = query.Where(x => x.Tipo == type);
        return (await query.ToListAsync()).OrderBy(x => x.Descricao).Select(Map).ToList();
    }

    public async Task<RecurrenceResponse> GetAsync(Guid id) => Map(await FindAsync(id));

    public async Task<RecurrenceResponse> CreateAsync(RecurrenceRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        var day = request.DiaReferencia is >= 1 and <= 31 ? request.DiaReferencia.Value : request.DataInicio.Day;
        var recurrence = new Recorrencia
        {
            MembroId = request.MembroId, ContaId = request.ContaId, CategoriaId = request.CategoriaId, Tipo = request.Tipo,
            Descricao = request.Descricao.Trim(), ValorCentavos = request.ValorCentavos, Frequencia = request.Frequencia,
            DataInicio = RecurrenceCycle.Normalize(request.DataInicio), DataFim = request.DataFim.HasValue ? RecurrenceCycle.Normalize(request.DataFim.Value) : null,
            ProximaOcorrencia = RecurrenceCycle.Normalize(request.DataInicio), DiaReferencia = day, GerarAutomaticamente = request.GerarAutomaticamente,
            ValorVariavel = request.ValorVariavel, Classificacao = request.Classificacao, Observacao = Trim(request.Observacao)
        };
        db.Recorrencias.Add(recurrence);
        await audit.RecordAsync("RECORRENCIA", recurrence.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(recurrence), usuarioId);
        await db.SaveChangesAsync();
        await GenerateForRecurrenceAsync(recurrence, DateTimeOffset.UtcNow);
        return await GetAsync(recurrence.Id);
    }

    public async Task<RecurrenceResponse> UpdateAsync(Guid id, RecurrenceRequest request, Guid? usuarioId)
    {
        await ValidateAsync(request);
        var recurrence = await FindAsync(id); var before = Snapshot(recurrence);
        recurrence.MembroId = request.MembroId; recurrence.ContaId = request.ContaId; recurrence.CategoriaId = request.CategoriaId; recurrence.Tipo = request.Tipo;
        recurrence.Descricao = request.Descricao.Trim(); recurrence.ValorCentavos = request.ValorCentavos; recurrence.Frequencia = request.Frequencia;
        recurrence.DataInicio = RecurrenceCycle.Normalize(request.DataInicio); recurrence.DataFim = request.DataFim.HasValue ? RecurrenceCycle.Normalize(request.DataFim.Value) : null;
        recurrence.DiaReferencia = request.DiaReferencia is >= 1 and <= 31 ? request.DiaReferencia.Value : request.DataInicio.Day;
        recurrence.GerarAutomaticamente = request.GerarAutomaticamente; recurrence.ValorVariavel = request.ValorVariavel; recurrence.Classificacao = request.Classificacao; recurrence.Observacao = Trim(request.Observacao);
        var today = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow);
        var futureForecasts = recurrence.Ocorrencias.Where(x => x.DataPrevista >= today && x.Status is StatusOcorrenciaRecorrencia.PENDENTE or StatusOcorrenciaRecorrencia.ATRASADA).ToList();
        foreach (var forecastOccurrence in futureForecasts)
        {
            forecastOccurrence.ValorPrevistoCentavos = request.ValorCentavos;
            if (forecastOccurrence.LancamentoId.HasValue)
            {
                var forecastLedger = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == forecastOccurrence.LancamentoId.Value);
                if (forecastLedger is not null) { forecastLedger.ValorCentavos = request.ValorCentavos; forecastLedger.ValorPrevistoCentavos = request.ValorCentavos; }
            }
        }
        await audit.RecordAsync("RECORRENCIA", recurrence.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(recurrence), usuarioId);
        await db.SaveChangesAsync();
        if (recurrence.Status == StatusRecorrencia.ATIVA) await GenerateForRecurrenceAsync(recurrence, DateTimeOffset.UtcNow);
        return await GetAsync(id);
    }

    public async Task<RecurrenceResponse> PauseAsync(Guid id, Guid? usuarioId) => await SetStatusAsync(id, StatusRecorrencia.PAUSADA, usuarioId);
    public async Task<RecurrenceResponse> ReactivateAsync(Guid id, Guid? usuarioId)
    {
        var result = await SetStatusAsync(id, StatusRecorrencia.ATIVA, usuarioId);
        await GenerateForRecurrenceAsync(await FindAsync(id), DateTimeOffset.UtcNow);
        return result;
    }
    public async Task<RecurrenceResponse> CloseAsync(Guid id, DateTimeOffset? endDate, Guid? usuarioId)
    {
        var recurrence = await FindAsync(id); var before = Snapshot(recurrence);
        recurrence.Status = StatusRecorrencia.ENCERRADA; recurrence.DataFim = RecurrenceCycle.Normalize(endDate ?? DateTimeOffset.UtcNow);
        await audit.RecordAsync("RECORRENCIA", recurrence.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(recurrence), usuarioId);
        await db.SaveChangesAsync();
        return Map(recurrence);
    }

    public async Task<IReadOnlyList<RecurrenceOccurrenceResponse>> ListOccurrencesAsync(OccurrenceQueryRequest request)
    {
        await GenerateAllAsync(DateTimeOffset.UtcNow);
        var rows = await db.OcorrenciasRecorrentes.AsNoTracking().Include(x => x.Recorrencia).ToListAsync();
        // Mantém o filtro de datas no processo: a comparação DateTimeOffset não é
        // traduzida pelo provider SQLite usado pela aplicação local.
        var query = rows.AsEnumerable();
        if (request.RecorrenciaId.HasValue) query = query.Where(x => x.RecorrenciaId == request.RecorrenciaId);
        if (request.DataInicio.HasValue) query = query.Where(x => x.DataPrevista >= request.DataInicio.Value);
        if (request.DataFim.HasValue) query = query.Where(x => x.DataPrevista < request.DataFim.Value.AddDays(1));
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.Tipo.HasValue) query = query.Where(x => x.Recorrencia!.Tipo == request.Tipo);
        return query.OrderBy(x => x.DataPrevista).Select(Map).ToList();
    }

    public async Task<RecurrenceOccurrenceResponse> GetOccurrenceAsync(Guid id) => Map(await FindOccurrenceAsync(id));

    public async Task<RecurrenceOccurrenceResponse> EffectAsync(Guid id, EffectOccurrenceRequest request, Guid? usuarioId)
    {
        var occurrence = await FindOccurrenceAsync(id);
        if (occurrence.Status is StatusOcorrenciaRecorrencia.EFETIVADA or StatusOcorrenciaRecorrencia.IGNORADA or StatusOcorrenciaRecorrencia.CANCELADA || occurrence.TransacaoId.HasValue || occurrence.CompraCartaoId.HasValue) throw new DomainException("Esta ocorrência não pode mais ser efetivada.");
        var value = request.ValorRealizadoCentavos > 0 ? request.ValorRealizadoCentavos : occurrence.ValorPrevistoCentavos;
        var date = RecurrenceCycle.Normalize(request.DataEfetivacao ?? occurrence.DataPrevista);
        var recurrence = occurrence.Recorrencia!;
        var forecast = occurrence.LancamentoId.HasValue ? await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == occurrence.LancamentoId.Value) : null;
        Guid transactionId;
        if (forecast is not null)
        {
            forecast.Status = StatusLancamento.EFETIVADO; forecast.ValorCentavos = value; forecast.DataEfetivacao = date; forecast.ValorPrevistoCentavos ??= occurrence.ValorPrevistoCentavos;
            var transaction = new Transacao { Id = forecast.Id, FamiliaId = forecast.FamiliaId, MembroId = recurrence.MembroId, ContaId = recurrence.ContaId, CategoriaId = recurrence.CategoriaId, Tipo = recurrence.Tipo, Descricao = recurrence.Descricao, ValorCentavos = value, DataCompetencia = occurrence.DataPrevista, DataMovimentacao = date, Status = StatusTransacao.EFETIVADA, Observacao = Trim(request.Observacao) ?? recurrence.Observacao, LancamentoId = forecast.Id };
            db.Transacoes.Add(transaction); transactionId = transaction.Id;
        }
        else
        {
            var transaction = await transactions.CreateAsync(new TransactionRequest(recurrence.Tipo, recurrence.Descricao, value, occurrence.DataPrevista, date, recurrence.ContaId, recurrence.CategoriaId, recurrence.MembroId, StatusTransacao.EFETIVADA, Trim(request.Observacao) ?? recurrence.Observacao), usuarioId); transactionId = transaction.Id; occurrence.LancamentoId = transaction.Id;
        }
        var before = Snapshot(occurrence);
        occurrence.ValorRealizadoCentavos = value; occurrence.TransacaoId = transactionId; occurrence.Status = StatusOcorrenciaRecorrencia.EFETIVADA; occurrence.Observacao = Trim(request.Observacao) ?? occurrence.Observacao;
        await audit.RecordAsync("OCORRENCIA_RECORRENCIA", occurrence.Id, OperacaoAuditoria.EFETIVACAO, before, Snapshot(occurrence), usuarioId);
        await db.SaveChangesAsync();
        return Map(occurrence);
    }

    public async Task<RecurrenceOccurrenceResponse> IgnoreAsync(Guid id, Guid? usuarioId)
    {
        var occurrence = await FindOccurrenceAsync(id);
        if (occurrence.Status == StatusOcorrenciaRecorrencia.EFETIVADA) throw new DomainException("Uma ocorrência efetivada não pode ser ignorada.");
        var before = Snapshot(occurrence); occurrence.Status = StatusOcorrenciaRecorrencia.IGNORADA;
        if (occurrence.LancamentoId.HasValue) { var forecast = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == occurrence.LancamentoId.Value); if (forecast is not null) forecast.Status = StatusLancamento.CANCELADO; }
        await audit.RecordAsync("OCORRENCIA_RECORRENCIA", occurrence.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(occurrence), usuarioId);
        await db.SaveChangesAsync(); return Map(occurrence);
    }

    public async Task<RecurrenceOccurrenceResponse> EditOccurrenceAsync(Guid id, EditOccurrenceRequest request, Guid? usuarioId)
    {
        if (request.ValorPrevistoCentavos <= 0) throw new DomainException("O valor previsto deve ser maior que zero.");
        var occurrence = await FindOccurrenceAsync(id);
        if (occurrence.Status is StatusOcorrenciaRecorrencia.EFETIVADA or StatusOcorrenciaRecorrencia.IGNORADA or StatusOcorrenciaRecorrencia.CANCELADA) throw new DomainException("Somente uma ocorrência pendente ou atrasada pode receber exceção.");
        var before = Snapshot(occurrence); occurrence.ValorPrevistoCentavos = request.ValorPrevistoCentavos; occurrence.DataPrevista = request.DataPrevista.HasValue ? RecurrenceCycle.Normalize(request.DataPrevista.Value) : occurrence.DataPrevista; occurrence.Observacao = Trim(request.Observacao);
        if (occurrence.LancamentoId.HasValue) { var forecast = await db.Lancamentos.SingleOrDefaultAsync(x => x.Id == occurrence.LancamentoId.Value); if (forecast is not null) { forecast.ValorCentavos = request.ValorPrevistoCentavos; forecast.ValorPrevistoCentavos = request.ValorPrevistoCentavos; forecast.DataCompetencia = occurrence.DataPrevista; forecast.DataVencimento = occurrence.DataPrevista; } }
        await audit.RecordAsync("OCORRENCIA_RECORRENCIA", occurrence.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(occurrence), usuarioId);
        await db.SaveChangesAsync(); return Map(occurrence);
    }

    public async Task<IReadOnlyList<RecurrenceOccurrenceResponse>> UpcomingAsync(int days = 30)
    {
        var start = RecurrenceCycle.Normalize(DateTimeOffset.UtcNow); var end = start.AddDays(Math.Clamp(days, 1, 730));
        return await ListOccurrencesAsync(new OccurrenceQueryRequest(start, end));
    }

    public async Task<RecurrenceSummaryResponse> SummaryAsync()
    {
        await GenerateAllAsync(DateTimeOffset.UtcNow);
        var active = await db.Recorrencias.AsNoTracking().Where(x => x.Status == StatusRecorrencia.ATIVA).ToListAsync();
        var incomes = active.Where(x => x.Tipo == TipoTransacao.RECEITA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var expenses = active.Where(x => x.Tipo == TipoTransacao.DESPESA).Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Frequencia));
        var accounts = await db.Contas.AsNoTracking().Where(x => x.Ativo).ToListAsync();
        var ledgerRows = await db.Lancamentos.AsNoTracking().Where(x => x.Status == StatusLancamento.EFETIVADO && accounts.Select(a => a.Id).Contains(x.ContaId)).ToListAsync();
        var real = accounts.Sum(x => x.SaldoInicialCentavos) + ledgerRows.Sum(x => FinanceCalculator.LedgerDelta(x.Tipo, x.Natureza, x.OrigemTipo, x.ValorCentavos));
        var forecasts = await db.Lancamentos.AsNoTracking().Where(x => x.Status == StatusLancamento.PREVISTO && x.OrigemTipo == OrigemLancamento.RECORRENCIA && accounts.Select(a => a.Id).Contains(x.ContaId)).ToListAsync();
        var projection = forecasts.Sum(x => FinanceCalculator.LedgerDelta(x.Tipo, x.Natureza, x.OrigemTipo, x.ValorCentavos));
        return new RecurrenceSummaryResponse(incomes, expenses, incomes - expenses, real, real + projection);
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> ListSubscriptionsAsync()
    {
        await GenerateAllAsync(DateTimeOffset.UtcNow);
        return (await db.Assinaturas.AsNoTracking().Include(x => x.Membro).Include(x => x.Categoria).Include(x => x.Conta).Include(x => x.Cartao).ToListAsync()).OrderBy(x => x.Nome).Select(Map).ToList();
    }

    public async Task<SubscriptionResponse> GetSubscriptionAsync(Guid id) => Map(await FindSubscriptionAsync(id));

    public async Task<SubscriptionResponse> CreateSubscriptionAsync(SubscriptionRequest request, Guid? usuarioId)
    {
        await ValidateSubscriptionAsync(request);
        var subscription = new Assinatura { MembroId = request.MembroId, Nome = request.Nome.Trim(), CategoriaId = request.CategoriaId, ContaId = request.ContaId, ValorCentavos = request.ValorCentavos, Periodicidade = request.Periodicidade, DataInicio = RecurrenceCycle.Normalize(request.DataInicio), ProximaCobranca = RecurrenceCycle.Normalize(request.ProximaCobranca), MetodoPagamento = request.MetodoPagamento, CartaoId = request.CartaoId, Observacao = Trim(request.Observacao) };
        db.Assinaturas.Add(subscription); await audit.RecordAsync("ASSINATURA", subscription.Id, OperacaoAuditoria.CRIACAO, null, Snapshot(subscription), usuarioId); await db.SaveChangesAsync();
        await GenerateSubscriptionChargesAsync(subscription, DateTimeOffset.UtcNow, usuarioId); return await GetSubscriptionAsync(subscription.Id);
    }

    public async Task<SubscriptionResponse> UpdateSubscriptionAsync(Guid id, SubscriptionRequest request, Guid? usuarioId)
    {
        await ValidateSubscriptionAsync(request); var subscription = await FindSubscriptionAsync(id); var before = Snapshot(subscription);
        subscription.MembroId = request.MembroId; subscription.Nome = request.Nome.Trim(); subscription.CategoriaId = request.CategoriaId; subscription.ContaId = request.ContaId; subscription.ValorCentavos = request.ValorCentavos; subscription.Periodicidade = request.Periodicidade; subscription.DataInicio = RecurrenceCycle.Normalize(request.DataInicio); subscription.ProximaCobranca = RecurrenceCycle.Normalize(request.ProximaCobranca); subscription.MetodoPagamento = request.MetodoPagamento; subscription.CartaoId = request.CartaoId; subscription.Observacao = Trim(request.Observacao);
        await audit.RecordAsync("ASSINATURA", subscription.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(subscription), usuarioId); await db.SaveChangesAsync(); return await GetSubscriptionAsync(id);
    }

    public async Task<SubscriptionResponse> SetSubscriptionStatusAsync(Guid id, bool active, Guid? usuarioId)
    {
        var subscription = await FindSubscriptionAsync(id); var before = Snapshot(subscription); subscription.Ativo = active; subscription.DataCancelamento = active ? null : DateTimeOffset.UtcNow;
        await audit.RecordAsync("ASSINATURA", subscription.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(subscription), usuarioId); await db.SaveChangesAsync(); return Map(subscription);
    }

    public async Task<SubscriptionSummaryResponse> SubscriptionSummaryAsync()
    {
        var active = await db.Assinaturas.AsNoTracking().Where(x => x.Ativo).ToListAsync();
        var monthly = active.Sum(x => RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Periodicidade));
        return new SubscriptionSummaryResponse(monthly, monthly * 12, active.Count);
    }

    public async Task GenerateAllAsync(DateTimeOffset now, Guid? usuarioId = null)
    {
        var today = RecurrenceCycle.Normalize(now);
        // O provedor SQLite do EF não traduz comparações de DateTimeOffset.
        // Carregamos somente as pendências e aplicamos o comparativo de data em memória.
        var overdue = (await db.OcorrenciasRecorrentes.Where(x => x.Status == StatusOcorrenciaRecorrencia.PENDENTE).ToListAsync()).Where(x => x.DataPrevista < today).ToList();
        foreach (var occurrence in overdue) occurrence.Status = StatusOcorrenciaRecorrencia.ATRASADA;
        if (overdue.Count > 0) await db.SaveChangesAsync();
        var recurrences = await db.Recorrencias.Where(x => x.Status == StatusRecorrencia.ATIVA && x.GerarAutomaticamente).ToListAsync();
        foreach (var recurrence in recurrences) await GenerateForRecurrenceAsync(recurrence, now);
        var subscriptions = (await db.Assinaturas.Where(x => x.Ativo).ToListAsync()).Where(x => x.ProximaCobranca <= now).ToList();
        foreach (var subscription in subscriptions) await GenerateSubscriptionChargesAsync(subscription, now, usuarioId);
    }

    private async Task GenerateForRecurrenceAsync(Recorrencia recurrence, DateTimeOffset now)
    {
        if (recurrence.Status != StatusRecorrencia.ATIVA) return;
        var horizon = RecurrenceCycle.Normalize(now).AddMonths(DefaultHorizonMonths);
        var date = RecurrenceCycle.FirstOnOrAfter(recurrence.DataInicio, recurrence.Frequencia, recurrence.DiaReferencia, recurrence.DataInicio);
        var existingDates = (await db.OcorrenciasRecorrentes.Where(x => x.RecorrenciaId == recurrence.Id).Select(x => x.DataPrevista).ToListAsync()).Where(x => x <= horizon).Select(x => x.Date).ToHashSet();
        var created = false;
        while (date <= horizon && (!recurrence.DataFim.HasValue || date <= recurrence.DataFim.Value))
        {
            if (!existingDates.Contains(date.Date))
            {
                var occurrence = new OcorrenciaRecorrencia { RecorrenciaId = recurrence.Id, DataPrevista = date, ValorPrevistoCentavos = recurrence.ValorCentavos, Status = date < RecurrenceCycle.Normalize(now) ? StatusOcorrenciaRecorrencia.ATRASADA : StatusOcorrenciaRecorrencia.PENDENTE };
                db.OcorrenciasRecorrentes.Add(occurrence); await db.SaveChangesAsync();
                var forecast = new Lancamento { Id = occurrence.Id, FamiliaId = recurrence.FamiliaId, MembroId = recurrence.MembroId, ContaId = recurrence.ContaId, CategoriaId = recurrence.CategoriaId, Tipo = recurrence.Tipo == TipoTransacao.RECEITA ? TipoLancamento.RECEITA : TipoLancamento.DESPESA, Natureza = recurrence.Tipo == TipoTransacao.RECEITA ? NaturezaLancamento.RENDA : NaturezaLancamento.CONSUMO, Status = StatusLancamento.PREVISTO, OrigemTipo = OrigemLancamento.RECORRENCIA, OrigemId = occurrence.Id, Descricao = recurrence.Descricao, ValorCentavos = recurrence.ValorCentavos, ValorPrevistoCentavos = recurrence.ValorCentavos, DataCompetencia = date, DataVencimento = date, Observacao = recurrence.Observacao };
                occurrence.LancamentoId = forecast.Id; db.Lancamentos.Add(forecast); await db.SaveChangesAsync(); created = true;
            }
            date = RecurrenceCycle.Next(date, recurrence.Frequencia, recurrence.DiaReferencia);
        }
        recurrence.ProximaOcorrencia = RecurrenceCycle.FirstOnOrAfter(recurrence.DataInicio, recurrence.Frequencia, recurrence.DiaReferencia, RecurrenceCycle.Normalize(now));
        if (created || db.Entry(recurrence).State == EntityState.Modified) await db.SaveChangesAsync();
    }

    private async Task GenerateSubscriptionChargesAsync(Assinatura subscription, DateTimeOffset now, Guid? usuarioId)
    {
        var guard = 0;
        while (subscription.Ativo && subscription.ProximaCobranca <= RecurrenceCycle.Normalize(now) && guard++ < 48)
        {
            var date = subscription.ProximaCobranca;
            if (subscription.MetodoPagamento == MetodoPagamentoAssinatura.CARTAO)
            {
                if (!subscription.CartaoId.HasValue) throw new DomainException("A assinatura em cartão precisa informar um cartão ativo.");
                var purchase = await cards.CreatePurchaseAsync(new CardPurchaseRequest(subscription.CartaoId.Value, subscription.MembroId, subscription.CategoriaId, subscription.Nome, subscription.ValorCentavos, 1, date, $"Assinatura {subscription.Nome}"), usuarioId);
                await audit.RecordAsync("ASSINATURA_COBRANCA", subscription.Id, OperacaoAuditoria.CRIACAO, null, new { AssinaturaId = subscription.Id, CompraCartaoId = purchase.Id, date, Metodo = "CARTAO" }, usuarioId);
            }
            else
            {
                var transaction = await transactions.CreateAsync(new TransactionRequest(TipoTransacao.DESPESA, subscription.Nome, subscription.ValorCentavos, date, date, subscription.ContaId, subscription.CategoriaId, subscription.MembroId, StatusTransacao.PREVISTA, $"Assinatura {subscription.Nome}"), usuarioId);
                await audit.RecordAsync("ASSINATURA_COBRANCA", subscription.Id, OperacaoAuditoria.CRIACAO, null, new { AssinaturaId = subscription.Id, TransacaoId = transaction.Id, date, Metodo = "CONTA" }, usuarioId);
            }
            subscription.ProximaCobranca = RecurrenceCycle.Next(subscription.ProximaCobranca, subscription.Periodicidade);
            await db.SaveChangesAsync();
        }
    }

    private async Task<RecurrenceResponse> SetStatusAsync(Guid id, StatusRecorrencia status, Guid? usuarioId)
    {
        var recurrence = await FindAsync(id); var before = Snapshot(recurrence); recurrence.Status = status;
        await audit.RecordAsync("RECORRENCIA", recurrence.Id, OperacaoAuditoria.ALTERACAO, before, Snapshot(recurrence), usuarioId); await db.SaveChangesAsync(); return Map(recurrence);
    }

    private async Task ValidateAsync(RecurrenceRequest request)
    {
        var errors = new List<string>();
        if (request.MembroId == Guid.Empty) errors.Add("O membro é obrigatório."); if (request.ContaId == Guid.Empty) errors.Add("A conta é obrigatória."); if (request.CategoriaId == Guid.Empty) errors.Add("A categoria é obrigatória.");
        if (request.Tipo is not (TipoTransacao.RECEITA or TipoTransacao.DESPESA)) errors.Add("A recorrência deve ser receita ou despesa."); if (string.IsNullOrWhiteSpace(request.Descricao)) errors.Add("A descrição é obrigatória."); if (request.ValorCentavos <= 0) errors.Add("O valor deve ser maior que zero."); if (request.DataFim.HasValue && request.DataFim < request.DataInicio) errors.Add("A data final não pode ser anterior ao início.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar a recorrência.", [.. errors]);
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("O membro deve existir e estar ativo.");
        if (!await db.Contas.AnyAsync(x => x.Id == request.ContaId && x.Ativo)) throw new DomainException("A conta deve existir e estar ativa.");
        var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == request.CategoriaId && x.Ativo) ?? throw new DomainException("A categoria deve existir e estar ativa.");
        if (category.Tipo != (request.Tipo == TipoTransacao.RECEITA ? TipoCategoria.Receita : TipoCategoria.Despesa)) throw new DomainException("A categoria deve ser compatível com o tipo da recorrência.");
    }

    private async Task ValidateSubscriptionAsync(SubscriptionRequest request)
    {
        var errors = new List<string>();
        if (request.MembroId == Guid.Empty || request.ContaId == Guid.Empty || request.CategoriaId == Guid.Empty) errors.Add("Membro, conta e categoria são obrigatórios.");
        if (string.IsNullOrWhiteSpace(request.Nome)) errors.Add("O nome da assinatura é obrigatório."); if (request.ValorCentavos <= 0) errors.Add("O valor deve ser maior que zero.");
        if (request.MetodoPagamento == MetodoPagamentoAssinatura.CARTAO && !request.CartaoId.HasValue) errors.Add("Informe o cartão para cobrança no cartão.");
        if (errors.Count > 0) throw new DomainException("Não foi possível salvar a assinatura.", [.. errors]);
        if (!await db.Membros.AnyAsync(x => x.Id == request.MembroId && x.Ativo)) throw new DomainException("O membro deve existir e estar ativo.");
        if (!await db.Contas.AnyAsync(x => x.Id == request.ContaId && x.Ativo)) throw new DomainException("A conta deve existir e estar ativa.");
        if (!await db.Categorias.AnyAsync(x => x.Id == request.CategoriaId && x.Ativo && x.Tipo == TipoCategoria.Despesa)) throw new DomainException("A categoria da assinatura deve ser uma despesa ativa.");
        if (request.CartaoId.HasValue && !await db.Cartoes.AnyAsync(x => x.Id == request.CartaoId && x.Ativo)) throw new DomainException("O cartão da assinatura deve existir e estar ativo.");
    }

    private async Task<Recorrencia> FindAsync(Guid id) => await db.Recorrencias.Include(x => x.Membro).Include(x => x.Conta).Include(x => x.Categoria).Include(x => x.Ocorrencias).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Recorrência não encontrada.");
    private async Task<OcorrenciaRecorrencia> FindOccurrenceAsync(Guid id) => await db.OcorrenciasRecorrentes.Include(x => x.Recorrencia).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Ocorrência não encontrada.");
    private async Task<Assinatura> FindSubscriptionAsync(Guid id) => await db.Assinaturas.Include(x => x.Membro).Include(x => x.Categoria).Include(x => x.Conta).Include(x => x.Cartao).SingleOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Assinatura não encontrada.");
    private static RecurrenceResponse Map(Recorrencia x) => new(x.Id, x.MembroId, x.Membro?.Nome ?? "", x.ContaId, x.Conta?.Nome ?? "", x.CategoriaId, x.Categoria?.Nome ?? "", x.Tipo, x.Descricao, x.ValorCentavos, x.Frequencia, x.DataInicio, x.DataFim, x.ProximaOcorrencia, x.DiaReferencia, x.GerarAutomaticamente, x.ValorVariavel, x.Classificacao, x.Status, x.Observacao, x.Ativo);
    private static RecurrenceOccurrenceResponse Map(OcorrenciaRecorrencia x) => new(x.Id, x.RecorrenciaId, x.Recorrencia?.Descricao ?? "", x.Recorrencia?.Tipo ?? TipoTransacao.DESPESA, x.Recorrencia?.Classificacao ?? TipoRecorrencia.NORMAL, x.DataPrevista, x.ValorPrevistoCentavos, x.ValorRealizadoCentavos, x.Status, x.TransacaoId, x.CompraCartaoId, x.Observacao);
    private static SubscriptionResponse Map(Assinatura x) { var monthly = RecurrenceCycle.MonthlyEquivalent(x.ValorCentavos, x.Periodicidade); return new(x.Id, x.MembroId, x.Membro?.Nome ?? "", x.Nome, x.CategoriaId, x.Categoria?.Nome ?? "", x.ContaId, x.Conta?.Nome ?? "", x.ValorCentavos, x.Periodicidade, x.DataInicio, x.ProximaCobranca, x.MetodoPagamento, x.CartaoId, x.Cartao?.Nome, x.Ativo, x.DataCancelamento, x.Observacao, monthly, monthly * 12); }
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static object Snapshot(Recorrencia x) => new { x.Id, x.Tipo, x.Descricao, x.ValorCentavos, x.Frequencia, x.DataInicio, x.DataFim, x.ProximaOcorrencia, x.Status, x.Classificacao };
    private static object Snapshot(OcorrenciaRecorrencia x) => new { x.Id, x.RecorrenciaId, x.DataPrevista, x.ValorPrevistoCentavos, x.ValorRealizadoCentavos, x.Status, x.TransacaoId, x.CompraCartaoId };
    private static object Snapshot(Assinatura x) => new { x.Id, x.Nome, x.ValorCentavos, x.Periodicidade, x.ProximaCobranca, x.MetodoPagamento, x.CartaoId, x.Ativo };
}

public static class RecurrenceCycle
{
    public static DateTimeOffset Normalize(DateTimeOffset value) => new(value.Year, value.Month, value.Day, 12, 0, 0, TimeSpan.Zero);
    public static DateTimeOffset Next(DateTimeOffset date, FrequenciaRecorrencia frequency, int dayReference) => frequency switch
    {
        FrequenciaRecorrencia.DIARIA => Normalize(date.AddDays(1)), FrequenciaRecorrencia.SEMANAL => Normalize(date.AddDays(7)), FrequenciaRecorrencia.QUINZENAL => Normalize(date.AddDays(14)),
        FrequenciaRecorrencia.MENSAL => Month(date, 1, dayReference), FrequenciaRecorrencia.BIMESTRAL => Month(date, 2, dayReference), FrequenciaRecorrencia.TRIMESTRAL => Month(date, 3, dayReference), FrequenciaRecorrencia.SEMESTRAL => Month(date, 6, dayReference), FrequenciaRecorrencia.ANUAL => Month(date, 12, dayReference), _ => Normalize(date.AddMonths(1))
    };
    public static DateTimeOffset Next(DateTimeOffset date, PeriodicidadeAssinatura frequency) => frequency switch { PeriodicidadeAssinatura.MENSAL => Month(date, 1, date.Day), PeriodicidadeAssinatura.BIMESTRAL => Month(date, 2, date.Day), PeriodicidadeAssinatura.TRIMESTRAL => Month(date, 3, date.Day), PeriodicidadeAssinatura.SEMESTRAL => Month(date, 6, date.Day), PeriodicidadeAssinatura.ANUAL => Month(date, 12, date.Day), _ => Month(date, 1, date.Day) };
    public static DateTimeOffset FirstOnOrAfter(DateTimeOffset start, FrequenciaRecorrencia frequency, int dayReference, DateTimeOffset target) { var value = Normalize(start); var targetDate = Normalize(target); while (value < targetDate) value = Next(value, frequency, dayReference); return value; }
    public static long MonthlyEquivalent(long cents, FrequenciaRecorrencia frequency) => frequency switch { FrequenciaRecorrencia.DIARIA => cents * 30, FrequenciaRecorrencia.SEMANAL => (long)Math.Round(cents * 52m / 12m, MidpointRounding.AwayFromZero), FrequenciaRecorrencia.QUINZENAL => (long)Math.Round(cents * 26m / 12m, MidpointRounding.AwayFromZero), FrequenciaRecorrencia.MENSAL => cents, FrequenciaRecorrencia.BIMESTRAL => cents / 2, FrequenciaRecorrencia.TRIMESTRAL => cents / 3, FrequenciaRecorrencia.SEMESTRAL => cents / 6, FrequenciaRecorrencia.ANUAL => cents / 12, _ => cents };
    public static long MonthlyEquivalent(long cents, PeriodicidadeAssinatura frequency) => frequency switch { PeriodicidadeAssinatura.MENSAL => cents, PeriodicidadeAssinatura.BIMESTRAL => cents / 2, PeriodicidadeAssinatura.TRIMESTRAL => cents / 3, PeriodicidadeAssinatura.SEMESTRAL => cents / 6, PeriodicidadeAssinatura.ANUAL => cents / 12, _ => cents };
    private static DateTimeOffset Month(DateTimeOffset date, int months, int referenceDay) { var moved = date.AddMonths(months); return new DateTimeOffset(moved.Year, moved.Month, Math.Min(Math.Max(referenceDay, 1), DateTime.DaysInMonth(moved.Year, moved.Month)), 12, 0, 0, TimeSpan.Zero); }
}
