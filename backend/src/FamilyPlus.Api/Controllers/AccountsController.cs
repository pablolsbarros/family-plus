using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/contas"), ServiceFilter<RequireSessionFilter>]
public sealed class AccountsController(AccountService service, TransactionService transactionService, AccountBalanceService balanceService) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<AccountResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<AccountResponse>>.Ok(await service.ListAsync()));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<AccountResponse>>> Get(Guid id) => Ok(ApiResponse<AccountResponse>.Ok(await service.GetAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AccountResponse>>> Create(AccountRequest request) => Ok(ApiResponse<AccountResponse>.Ok(await service.CreateAsync(request), "Conta cadastrada."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<AccountResponse>>> Update(Guid id, AccountRequest request) => Ok(ApiResponse<AccountResponse>.Ok(await service.UpdateAsync(id, request), "Conta atualizada."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<AccountResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<AccountResponse>.Ok(await service.SetStatusAsync(id, ativo), "Situação atualizada."));
    [HttpGet("{id:guid}/saldo")] public async Task<ActionResult<ApiResponse<AccountBalanceResponse>>> Balance(Guid id, [FromQuery] DateTimeOffset? data = null) => Ok(ApiResponse<AccountBalanceResponse>.Ok(await transactionService.BalanceAsync(id, data)));
    [HttpGet("{id:guid}/extrato")] public async Task<ActionResult<ApiResponse<AccountStatementResponse>>> Statement(Guid id, [FromQuery] string? dataInicio = null, [FromQuery] string? dataFim = null, [FromQuery] string? tipo = null, [FromQuery] string? categoria = null, [FromQuery] string? status = null) => Ok(ApiResponse<AccountStatementResponse>.Ok(await transactionService.StatementAsync(id, dataInicio, dataFim, tipo, categoria, status)));
    [HttpGet("{id:guid}/projecao")] public async Task<ActionResult<ApiResponse<LedgerBalanceResponse>>> Projection(Guid id, [FromQuery] DateTimeOffset? data = null) => Ok(ApiResponse<LedgerBalanceResponse>.Ok(await balanceService.ProjectionAsync(id, data)));
}
