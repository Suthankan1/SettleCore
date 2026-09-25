using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Modules.Ledger.Application;

public interface ILedgerRepository
{
    Task<IReadOnlyList<LedgerAccount>> GetAccountsByIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken = default);

    Task AddTransactionAsync(
        LedgerTransaction transaction,
        CancellationToken cancellationToken = default);
}
