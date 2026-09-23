using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/recorrencias"), ServiceFilter<RequireSessionFilter>]
public sealed class RecurrencesController(RecurrenceService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<RecurrenceResponse>>>> List([FromQuery] TipoTransacao? tipo = null) => Ok(ApiResponse<IReadOnlyList<RecurrenceResponse>>.Ok(await service.ListAsync(tipo)));
    [HttpGet("receitas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<RecurrenceResponse>>>> Incomes() => Ok(ApiResponse<IReadOnlyList<RecurrenceResponse>>.Ok(await service.ListAsync(TipoTransacao.RECEITA)));
    [HttpGet("despesas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<RecurrenceResponse>>>> Expenses() => Ok(ApiResponse<IReadOnlyList<RecurrenceResponse>>.Ok(await service.ListAsync(TipoTransacao.DESPESA)));
    [HttpGet("proximas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<RecurrenceOccurrenceResponse>>>> Upcoming([FromQuery] int dias = 30) => Ok(ApiResponse<IReadOnlyList<RecurrenceOccurrenceResponse>>.Ok(await service.UpcomingAsync(dias)));
    [HttpGet("resumo")] public async Task<ActionResult<ApiResponse<RecurrenceSummaryResponse>>> Summary() => Ok(ApiResponse<RecurrenceSummaryResponse>.Ok(await service.SummaryAsync()));
    [HttpPost("processar")] public async Task<ActionResult<ApiResponse<object>>> Process() { await service.GenerateAllAsync(DateTimeOffset.UtcNow, UserId()); return Ok(ApiResponse<object>.Ok(new { processadoEm = DateTimeOffset.UtcNow }, "Recorrências processadas de forma idempotente.")); }
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Get(Guid id) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Create(RecurrenceRequest request) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.CreateAsync(request, UserId()), "Recorrência cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Update(Guid id, RecurrenceRequest request) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.UpdateAsync(id, request, UserId()), "Próximas ocorrências atualizadas."));
    [HttpPatch("{id:guid}/pausar")] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Pause(Guid id) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.PauseAsync(id, UserId()), "Recorrência pausada."));
    [HttpPatch("{id:guid}/reativar")] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Reactivate(Guid id) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.ReactivateAsync(id, UserId()), "Recorrência reativada."));
    [HttpPatch("{id:guid}/encerrar")] public async Task<ActionResult<ApiResponse<RecurrenceResponse>>> Close(Guid id, [FromBody] DateTimeOffset? dataFim) => Ok(ApiResponse<RecurrenceResponse>.Ok(await service.CloseAsync(id, dataFim, UserId()), "Recorrência encerrada."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}

[ApiController, Route("api/ocorrencias-recorrentes"), ServiceFilter<RequireSessionFilter>]
public sealed class RecurrenceOccurrencesController(RecurrenceService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<RecurrenceOccurrenceResponse>>>> List([FromQuery] OccurrenceQueryRequest request) => Ok(ApiResponse<IReadOnlyList<RecurrenceOccurrenceResponse>>.Ok(await service.ListOccurrencesAsync(request)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<RecurrenceOccurrenceResponse>>> Get(Guid id) => Ok(ApiResponse<RecurrenceOccurrenceResponse>.Ok(await service.GetOccurrenceAsync(id)));
    [HttpPost("{id:guid}/efetivar")] public async Task<ActionResult<ApiResponse<RecurrenceOccurrenceResponse>>> Effect(Guid id, EffectOccurrenceRequest request) => Ok(ApiResponse<RecurrenceOccurrenceResponse>.Ok(await service.EffectAsync(id, request, UserId()), "Ocorrência efetivada e movimentação criada."));
    [HttpPatch("{id:guid}/ignorar")] public async Task<ActionResult<ApiResponse<RecurrenceOccurrenceResponse>>> Ignore(Guid id) => Ok(ApiResponse<RecurrenceOccurrenceResponse>.Ok(await service.IgnoreAsync(id, UserId()), "Ocorrência ignorada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<RecurrenceOccurrenceResponse>>> Update(Guid id, EditOccurrenceRequest request) => Ok(ApiResponse<RecurrenceOccurrenceResponse>.Ok(await service.EditOccurrenceAsync(id, request, UserId()), "Exceção da ocorrência atualizada."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}

[ApiController, Route("api/assinaturas"), ServiceFilter<RequireSessionFilter>]
public sealed class SubscriptionsController(RecurrenceService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<SubscriptionResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<SubscriptionResponse>>.Ok(await service.ListSubscriptionsAsync()));
    [HttpGet("resumo")] public async Task<ActionResult<ApiResponse<SubscriptionSummaryResponse>>> Summary() => Ok(ApiResponse<SubscriptionSummaryResponse>.Ok(await service.SubscriptionSummaryAsync()));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Get(Guid id) => Ok(ApiResponse<SubscriptionResponse>.Ok(await service.GetSubscriptionAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Create(SubscriptionRequest request) => Ok(ApiResponse<SubscriptionResponse>.Ok(await service.CreateSubscriptionAsync(request, UserId()), "Assinatura cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Update(Guid id, SubscriptionRequest request) => Ok(ApiResponse<SubscriptionResponse>.Ok(await service.UpdateSubscriptionAsync(id, request, UserId()), "Assinatura atualizada."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<SubscriptionResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<SubscriptionResponse>.Ok(await service.SetSubscriptionStatusAsync(id, ativo, UserId()), ativo ? "Assinatura reativada." : "Assinatura cancelada."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
