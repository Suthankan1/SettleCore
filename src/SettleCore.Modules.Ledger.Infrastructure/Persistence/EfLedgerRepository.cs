using Microsoft.EntityFrameworkCore;
using SettleCore.Modules.Ledger.Application;
using SettleCore.Modules.Ledger.Domain;
using SettleCore.Modules.Ledger.Infrastructure.Persistence.Records;

namespace SettleCore.Modules.Ledger.Infrastructure.Persistence;

public sealed class EfLedgerRepository(
    LedgerDbContext dbContext)
    : ILedgerRepository
{
    public async Task<IReadOnlyList<LedgerAccount>>
        GetAccountsByIdsAsync(
            IReadOnlyCollection<Guid> accountIds,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountIds);

        if (accountIds.Count == 0)
        {
            return [];
        }

        var ids = accountIds
            .Distinct()
            .ToArray();

        return await dbContext.LedgerAccounts
            .AsNoTracking()
            .Where(account => ids.Contains(account.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddTransactionAsync(
        LedgerTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var entries = transaction.Entries
            .Select(entry =>
                new LedgerEntryRecord(
                    Guid.NewGuid(),
                    transaction.Id,
                    entry.AccountId,
                    entry.Currency,
                    entry.Direction,
                    entry.AmountMinorUnits))
            .ToArray();

        var record = new LedgerTransactionRecord(
            transaction.Id,
            transaction.LedgerId,
            entries);

        dbContext.LedgerTransactions.Add(record);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
