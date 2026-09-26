namespace SettleCore.Modules.Ledger.Domain;

public static class PaymentLedgerPosting
{
    public static IReadOnlyList<LedgerEntry> Create(
        Guid processorReceivableAccountId,
        Guid merchantPayableAccountId,
        Guid platformRevenueAccountId,
        string currency,
        long grossAmountMinorUnits,
        long feeAmountMinorUnits)
    {
        var merchantAmountMinorUnits =
            grossAmountMinorUnits - feeAmountMinorUnits;

        return
        [
            LedgerEntry.Create(
                processorReceivableAccountId,
                currency,
                LedgerDirection.Debit,
                grossAmountMinorUnits),

            LedgerEntry.Create(
                merchantPayableAccountId,
                currency,
                LedgerDirection.Credit,
                merchantAmountMinorUnits),

            LedgerEntry.Create(
                platformRevenueAccountId,
                currency,
                LedgerDirection.Credit,
                feeAmountMinorUnits)
        ];
    }
}