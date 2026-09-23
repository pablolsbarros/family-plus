using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/dashboard"), ServiceFilter<RequireSessionFilter>]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet("resumo")] public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> Summary([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardSummaryResponse>.Ok(await service.GetSummaryAsync(request)));
    [HttpGet("saldo")] public async Task<ActionResult<ApiResponse<DashboardBalanceResponse>>> Balance([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardBalanceResponse>.Ok(await service.GetBalanceAsync(request)));
    [HttpGet("saldos-contas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardAccountResponse>>>> AccountBalances([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardAccountResponse>>.Ok(await service.GetAccountBalancesAsync(request)));
    [HttpGet("receitas")] public async Task<ActionResult<ApiResponse<DashboardPeriodResponse>>> Incomes([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardPeriodResponse>.Ok(await service.GetPeriodAsync(request)));
    [HttpGet("despesas")] public async Task<ActionResult<ApiResponse<DashboardPeriodResponse>>> Expenses([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardPeriodResponse>.Ok(await service.GetPeriodAsync(request)));
    [HttpGet("resultado")] public async Task<ActionResult<ApiResponse<DashboardPeriodResponse>>> Result([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardPeriodResponse>.Ok(await service.GetPeriodAsync(request)));
    [HttpGet("fluxo-caixa")] public async Task<ActionResult<ApiResponse<DashboardCashFlowResponse>>> CashFlow([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardCashFlowResponse>.Ok(await service.GetCashFlowAsync(request)));
    [HttpGet("saldo-projetado")] public async Task<ActionResult<ApiResponse<DashboardProjectionResponse>>> ProjectedBalance([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardProjectionResponse>.Ok(await service.GetProjectionAsync(request)));
    [HttpGet("proximas-receitas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardUpcomingResponse>>>> UpcomingIncomes([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardUpcomingResponse>>.Ok((await service.GetSummaryAsync(request)).ProximasReceitas));
    [HttpGet("proximas-despesas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardUpcomingResponse>>>> UpcomingExpenses([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardUpcomingResponse>>.Ok((await service.GetSummaryAsync(request)).ProximasDespesas));
    [HttpGet("cartoes")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardCardResponse>>>> Cards([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardCardResponse>>.Ok((await service.GetSummaryAsync(request)).Cartoes));
    [HttpGet("despesas-categorias")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardCategoryResponse>>>> ExpenseCategories([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardCategoryResponse>>.Ok((await service.GetSummaryAsync(request)).DespesasCategorias));
    [HttpGet("receitas-categorias")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardCategoryResponse>>>> IncomeCategories([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardCategoryResponse>>.Ok((await service.GetSummaryAsync(request)).ReceitasCategorias));
    [HttpGet("evolucao")] public async Task<ActionResult<ApiResponse<IReadOnlyList<DashboardMonthlyResponse>>>> Evolution([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<IReadOnlyList<DashboardMonthlyResponse>>.Ok((await service.GetSummaryAsync(request)).Evolucao));
    [HttpGet("kpis")] public async Task<ActionResult<ApiResponse<DashboardKpiResponse>>> Kpis([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<DashboardKpiResponse>.Ok(await service.GetKpisAsync(request)));
    [HttpGet("orcamento")] public async Task<ActionResult<ApiResponse<BudgetDashboardResponse?>>> Budget([FromQuery] DashboardQueryRequest request) => Ok(ApiResponse<BudgetDashboardResponse?>.Ok(await service.GetBudgetAsync(request)));
}
