using SettleCore.Modules.Ledger.Application.PostLedgerTransaction;
using SettleCore.Modules.Ledger.Domain;

namespace SettleCore.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/ledger/transactions",
                async Task<IResult> (
                    PostLedgerTransactionRequest request,
                    PostLedgerTransactionHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new PostLedgerTransactionCommand(
                        request.TransactionId,
                        request.LedgerId,
                        request.Entries
                            .Select(entry =>
                                new PostLedgerTransactionEntry(
                                    entry.AccountId,
                                    entry.Currency,
                                    entry.Direction,
                                    entry.AmountMinorUnits))
                            .ToArray());

                    var result = await handler.HandleAsync(
                        command,
                        cancellationToken);

                    return Results.Created(
                        $"/ledger/transactions/{result.TransactionId}",
                        result);
                })
            .WithName("PostLedgerTransaction")
            .Produces<PostLedgerTransactionResult>(
                StatusCodes.Status201Created);

        return endpoints;
    }
}

public sealed record PostLedgerTransactionRequest(
    Guid TransactionId,
    Guid LedgerId,
    IReadOnlyList<PostLedgerTransactionEntryRequest> Entries);

public sealed record PostLedgerTransactionEntryRequest(
    Guid AccountId,
    string Currency,
    LedgerDirection Direction,
    long AmountMinorUnits);