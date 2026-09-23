using SettleCore.Modules.Payments.Application.CreatePayment;

namespace SettleCore.Api.Endpoints;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/payments",
                async (
                    CreatePaymentRequest request,
                    CreatePaymentHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new CreatePaymentCommand(
                            request.Amount,
                            request.Currency),
                        cancellationToken);

                    return Results.Created(
                        $"/payments/{result.PaymentId}",
                        result);
                })
            .WithName("CreatePayment")
            .Produces<CreatePaymentResult>(
                StatusCodes.Status201Created);

        return endpoints;
    }
}

public sealed record CreatePaymentRequest(
    decimal Amount,
    string Currency);