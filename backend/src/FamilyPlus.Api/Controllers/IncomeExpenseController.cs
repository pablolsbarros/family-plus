using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, ServiceFilter<RequireSessionFilter>]
public sealed class IncomeController(TransactionService service) : ControllerBase
{
    [HttpGet("api/receitas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<TransactionResponse>>>> List([FromQuery] TransactionQueryRequest request) => Ok(ApiResponse<IReadOnlyList<TransactionResponse>>.Ok((await service.ListAsync(request with { Tipo = "RECEITA", Pagina = 1, TamanhoPagina = 1000 })).Items));
    [HttpGet("api/receitas/{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Get(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.GetAsync(id, TipoTransacao.RECEITA)));
    [HttpPost("api/receitas")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CreateAsync(request with { Tipo = TipoTransacao.RECEITA }, CurrentUserId()), "Receita cadastrada."));
    [HttpPut("api/receitas/{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Update(Guid id, TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.UpdateAsync(id, request with { Tipo = TipoTransacao.RECEITA }, CurrentUserId(), TipoTransacao.RECEITA), "Receita atualizada."));
    [HttpDelete("api/receitas/{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Delete(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CancelAsync(id, CurrentUserId(), TipoTransacao.RECEITA), "Receita cancelada."));
    [HttpPatch("api/receitas/{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Cancel(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CancelAsync(id, CurrentUserId(), TipoTransacao.RECEITA), "Receita cancelada."));
    private Guid? CurrentUserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}

[ApiController, ServiceFilter<RequireSessionFilter>]
public sealed class ExpenseController(TransactionService service) : ControllerBase
{
    [HttpGet("api/despesas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<TransactionResponse>>>> List([FromQuery] TransactionQueryRequest request) => Ok(ApiResponse<IReadOnlyList<TransactionResponse>>.Ok((await service.ListAsync(request with { Tipo = "DESPESA", Pagina = 1, TamanhoPagina = 1000 })).Items));
    [HttpGet("api/despesas/{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Get(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.GetAsync(id, TipoTransacao.DESPESA)));
    [HttpPost("api/despesas")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Create(TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CreateAsync(request with { Tipo = TipoTransacao.DESPESA }, CurrentUserId()), "Despesa cadastrada."));
    [HttpPut("api/despesas/{id:guid}")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Update(Guid id, TransactionRequest request) => Ok(ApiResponse<TransactionResponse>.Ok(await service.UpdateAsync(id, request with { Tipo = TipoTransacao.DESPESA }, CurrentUserId(), TipoTransacao.DESPESA), "Despesa atualizada."));
    [HttpPatch("api/despesas/{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<TransactionResponse>>> Cancel(Guid id) => Ok(ApiResponse<TransactionResponse>.Ok(await service.CancelAsync(id, CurrentUserId(), TipoTransacao.DESPESA), "Despesa cancelada."));
    private Guid? CurrentUserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
