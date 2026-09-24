using SettleCore.Modules.Payments.Application.CreatePayment;

namespace SettleCore.Api.Endpoints;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/payments",
                async Task<IResult> (
                    CreatePaymentRequest request,
                    CreatePaymentHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var result = await handler.HandleAsync(
                            new CreatePaymentCommand(
                                request.Amount,
                                request.Currency),
                            cancellationToken);

                        return Results.Created(
                            $"/payments/{result.PaymentId}",
                            result);
                    }
                    catch (ArgumentException exception)
                    {
                        var parameterName =
                            exception.ParamName ?? "payment";

                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                [parameterName] =
                                [
                                    exception.Message
                                ]
                            });
                    }
                })
            .WithName("CreatePayment")
            .Produces<CreatePaymentResult>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

public sealed record CreatePaymentRequest(
    decimal Amount,
    string Currency);