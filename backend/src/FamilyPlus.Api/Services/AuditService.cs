using System.Text.Json;
using System.Text.Json.Serialization;
using FamilyPlus.Api.Data;
using FamilyPlus.Api.Entities;

namespace FamilyPlus.Api.Services;

public sealed class AuditService(FinanceDbContext db)
{
    public Task RecordAsync(string entidade, Guid entidadeId, OperacaoAuditoria operacao, object? anterior, object? novo, Guid? usuarioId)
    {
        db.Auditorias.Add(new Auditoria
        {
            Entidade = entidade,
            EntidadeId = entidadeId,
            Operacao = operacao,
            DadosAnteriores = anterior is null ? null : JsonSerializer.Serialize(anterior, Options),
            DadosNovos = novo is null ? null : JsonSerializer.Serialize(novo, Options),
            UsuarioId = usuarioId
        });
        return Task.CompletedTask;
    }

    private static readonly JsonSerializerOptions Options = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
}
