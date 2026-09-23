using FamilyPlus.Api.Entities;

namespace FamilyPlus.Api.Services;

public static class FinanceCalculator
{
    public static long CalculateBalance(long saldoInicialCentavos, IEnumerable<(TipoTransacao Tipo, long ValorCentavos, StatusTransacao Status)> transacoes)
    {
        return transacoes.Where(x => x.Status == StatusTransacao.EFETIVADA).Aggregate(saldoInicialCentavos, (saldo, movimento) => saldo + Delta(movimento.Tipo, movimento.ValorCentavos));
    }

    public static long Delta(TipoTransacao tipo, long valorCentavos) => tipo switch
    {
        TipoTransacao.RECEITA or TipoTransacao.TRANSFERENCIA_ENTRADA => valorCentavos,
        TipoTransacao.DESPESA or TipoTransacao.TRANSFERENCIA_SAIDA => -valorCentavos,
        _ => 0
    };

    public static long CalculateLedgerBalance(long saldoInicialCentavos, IEnumerable<(TipoLancamento Tipo, NaturezaLancamento Natureza, StatusLancamento Status, OrigemLancamento Origem, long ValorCentavos)> lancamentos)
        => lancamentos.Where(x => x.Status == StatusLancamento.EFETIVADO).Aggregate(saldoInicialCentavos, (saldo, item) => saldo + LedgerDelta(item.Tipo, item.Natureza, item.Origem, item.ValorCentavos));

    public static long LedgerDelta(TipoLancamento tipo, NaturezaLancamento natureza, OrigemLancamento origem, long valorCentavos)
    {
        if (origem == OrigemLancamento.COMPRA_CARTAO) return 0;
        return tipo switch
        {
            TipoLancamento.RECEITA or TipoLancamento.TRANSFERENCIA_ENTRADA => Math.Abs(valorCentavos),
            TipoLancamento.DESPESA or TipoLancamento.TRANSFERENCIA_SAIDA or TipoLancamento.PAGAMENTO_FATURA => -Math.Abs(valorCentavos),
            TipoLancamento.AJUSTE => valorCentavos,
            _ => 0
        };
    }
}
