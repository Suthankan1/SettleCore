using System.Numerics;

namespace SettleCore.Modules.Ledger.Domain;

public sealed class LedgerTransaction
{
    private LedgerTransaction(
        Guid id,
        IReadOnlyList<LedgerEntry> entries)
    {
        Id = id;
        Entries = entries;
    }

    public Guid Id { get; }

    public IReadOnlyList<LedgerEntry> Entries { get; }

    public static LedgerTransaction Post(
        Guid id,
        IEnumerable<LedgerEntry> entries)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Ledger transaction ID must not be empty.",
                nameof(id));
        }

        ArgumentNullException.ThrowIfNull(entries);

        var snapshot = entries.ToArray();

        if (snapshot.Length < 2 || snapshot.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Ledger transaction requires at least two valid entries.",
                nameof(entries));
        }

        foreach (var currencyEntries in snapshot.GroupBy(
                     entry => entry.Currency,
                     StringComparer.Ordinal))
        {
            BigInteger debits = 0;
            BigInteger credits = 0;

            foreach (var entry in currencyEntries)
            {
                if (entry.Direction == LedgerDirection.Debit)
                {
                    debits += entry.AmountMinorUnits;
                }
                else
                {
                    credits += entry.AmountMinorUnits;
                }
            }

            if (debits != credits)
            {
                throw new ArgumentException(
                    $"Ledger entries are not balanced for {currencyEntries.Key}.",
                    nameof(entries));
            }
        }

        return new LedgerTransaction(id, Array.AsReadOnly(snapshot));
    }
}
