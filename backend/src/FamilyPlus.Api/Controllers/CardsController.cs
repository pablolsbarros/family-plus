using FamilyPlus.Api.DTOs;
using FamilyPlus.Api.Entities;
using FamilyPlus.Api.Filters;
using FamilyPlus.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FamilyPlus.Api.Controllers;

[ApiController, Route("api/cartoes"), ServiceFilter<RequireSessionFilter>]
public sealed class CardsController(CardService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<CardResponse>>>> List() => Ok(ApiResponse<IReadOnlyList<CardResponse>>.Ok(await service.ListCardsAsync()));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<CardResponse>>> Get(Guid id) => Ok(ApiResponse<CardResponse>.Ok(await service.GetCardAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<CardResponse>>> Create(CardRequest request) => Ok(ApiResponse<CardResponse>.Ok(await service.CreateCardAsync(request, UserId()), "Cartão cadastrado."));
    [HttpPut("{id:guid}")] public async Task<ActionResult<ApiResponse<CardResponse>>> Update(Guid id, CardRequest request) => Ok(ApiResponse<CardResponse>.Ok(await service.UpdateCardAsync(id, request, UserId()), "Cartão atualizado."));
    [HttpPatch("{id:guid}/status")] public async Task<ActionResult<ApiResponse<CardResponse>>> Status(Guid id, [FromBody] bool ativo) => Ok(ApiResponse<CardResponse>.Ok(await service.SetCardStatusAsync(id, ativo, UserId()), "Situação atualizada."));
    [HttpGet("{id:guid}/limite")] public async Task<ActionResult<ApiResponse<CardLimitResponse>>> Limit(Guid id) => Ok(ApiResponse<CardLimitResponse>.Ok(await service.GetLimitAsync(id)));
    [HttpGet("{id:guid}/faturas")] public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceResponse>>>> Invoices(Guid id) => Ok(ApiResponse<IReadOnlyList<InvoiceResponse>>.Ok(await service.ListCardInvoicesAsync(id)));
    [HttpGet("{id:guid}/projecao")] public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceProjectionResponse>>>> Projection(Guid id) => Ok(ApiResponse<IReadOnlyList<InvoiceProjectionResponse>>.Ok(await service.GetProjectionAsync(id)));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}

[ApiController, Route("api/compras-cartao"), ServiceFilter<RequireSessionFilter>]
public sealed class CardPurchasesController(CardService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<CardPurchaseResponse>>>> List([FromQuery] Guid? cartaoId = null) => Ok(ApiResponse<IReadOnlyList<CardPurchaseResponse>>.Ok(await service.ListPurchasesAsync(cartaoId)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<CardPurchaseResponse>>> Get(Guid id) => Ok(ApiResponse<CardPurchaseResponse>.Ok(await service.GetPurchaseAsync(id)));
    [HttpPost] public async Task<ActionResult<ApiResponse<CardPurchaseResponse>>> Create(CardPurchaseRequest request) => Ok(ApiResponse<CardPurchaseResponse>.Ok(await service.CreatePurchaseAsync(request, UserId()), "Compra no cartão cadastrada."));
    [HttpPatch("{id:guid}/cancelar")] public async Task<ActionResult<ApiResponse<CardPurchaseResponse>>> Cancel(Guid id) => Ok(ApiResponse<CardPurchaseResponse>.Ok(await service.CancelPurchaseAsync(id, UserId()), "Compra cancelada."));
    [HttpPost("{id:guid}/estornar")] public async Task<ActionResult<ApiResponse<CardPurchaseResponse>>> Refund(Guid id, RefundRequest request) => Ok(ApiResponse<CardPurchaseResponse>.Ok(await service.RefundPurchaseAsync(id, request, UserId()), "Estorno registrado."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}

[ApiController, Route("api/faturas"), ServiceFilter<RequireSessionFilter>]
public sealed class InvoicesController(CardService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<IReadOnlyList<InvoiceResponse>>>> List([FromQuery] Guid? cartaoId = null, [FromQuery] StatusFatura? status = null) => Ok(ApiResponse<IReadOnlyList<InvoiceResponse>>.Ok(await service.ListInvoicesAsync(cartaoId, status)));
    [HttpGet("{id:guid}")] public async Task<ActionResult<ApiResponse<InvoiceDetailResponse>>> Get(Guid id) => Ok(ApiResponse<InvoiceDetailResponse>.Ok(await service.GetInvoiceAsync(id)));
    [HttpPatch("{id:guid}/fechar")] public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Close(Guid id) => Ok(ApiResponse<InvoiceResponse>.Ok(await service.CloseInvoiceAsync(id, UserId()), "Fatura fechada."));
    [HttpPost("{id:guid}/pagar")] public async Task<ActionResult<ApiResponse<InvoiceResponse>>> Pay(Guid id, InvoicePaymentRequest request) => Ok(ApiResponse<InvoiceResponse>.Ok(await service.PayInvoiceAsync(id, request, UserId()), "Fatura paga e conta atualizada."));
    private Guid? UserId() => (HttpContext.Items["currentUser"] as SessionUser)?.Id;
}
