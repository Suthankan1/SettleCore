using SettleCore.Modules.Payments.Application.AttachProviderReference;
using SettleCore.Modules.Payments.Application.CreatePayment;
using SettleCore.Modules.Payments.Application.GetPaymentById;
using SettleCore.Modules.Payments.Application.MarkPaymentSucceeded;

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

        endpoints.MapGet(
                "/payments/{paymentId:guid}",
                async Task<IResult> (
                    Guid paymentId,
                    GetPaymentByIdHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var result = await handler.HandleAsync(
                            new GetPaymentByIdQuery(paymentId),
                            cancellationToken);

                        return result is null
                            ? Results.NotFound()
                            : Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        var parameterName =
                            exception.ParamName ?? "paymentId";

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
            .WithName("GetPaymentById")
            .Produces<GetPaymentByIdResult>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        endpoints.MapPost(
                "/payments/{paymentId:guid}/succeed",
                async Task<IResult> (
                    Guid paymentId,
                    MarkPaymentSucceededHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var result = await handler.HandleAsync(
                            new MarkPaymentSucceededCommand(paymentId),
                            cancellationToken);

                        return result is null
                            ? Results.NotFound()
                            : Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        var parameterName =
                            exception.ParamName ?? "paymentId";

                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                [parameterName] =
                                [
                                    exception.Message
                                ]
                            });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Conflict(
                            new
                            {
                                error = exception.Message
                            });
                    }
                })
            .WithName("MarkPaymentSucceeded")
            .Produces<MarkPaymentSucceededResult>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status409Conflict);

        endpoints.MapPost(
                "/payments/{paymentId:guid}/provider-reference",
                async Task<IResult> (
                    Guid paymentId,
                    AttachProviderReferenceRequest request,
                    AttachProviderReferenceHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var result = await handler.HandleAsync(
                            new AttachProviderReferenceCommand(
                                paymentId,
                                request.Provider,
                                request.Reference),
                            cancellationToken);

                        return result is null
                            ? Results.NotFound()
                            : Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        var parameterName =
                            exception.ParamName ?? "providerReference";

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
            .WithName("AttachProviderReference")
            .Produces<AttachProviderReferenceResult>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

public sealed record CreatePaymentRequest(
    decimal Amount,
    string Currency);

public sealed record AttachProviderReferenceRequest(
    string Provider,
    string Reference);
