using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Modules.Ledger.Application;

public interface ILedgerRepository
{
    Task AddTransactionAsync(
        LedgerTransaction transaction,
        CancellationToken cancellationToken = default);
}