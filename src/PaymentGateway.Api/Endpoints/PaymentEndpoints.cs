using MediatR;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Application.Payments.GetPayment;
using PaymentGateway.Application.Payments.ProcessPayment;

namespace PaymentGateway.Api.Endpoints
{
    public static class PaymentEndpoints
    {
        public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/v1/payments")
                .WithTags("Payments");

            group.MapPost("", ProcessPayment)
                .WithName("ProcessPayment")
                .WithSummary("Process a new payment")
                .Produces<ProcessPaymentResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

            group.MapGet("{id}", GetPayment)
                .WithName("GetPayment")
                .WithSummary("Retrieve payment details")
                .Produces<GetPaymentResponse>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

            return app;
        }

        private static async Task<IResult> ProcessPayment(
            [FromBody] ProcessPaymentRequest request,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var command = new ProcessPaymentCommand(
                request.CardNumber,
                request.ExpiryMonth,
                request.ExpiryYear,
                request.Currency,
                request.Amount,
                request.Cvv);

            var result = await sender.Send(command, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.Type switch
                {
                    Domain.Common.ErrorType.Validation => Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation Error",
                        Detail = error.Message,
                        Extensions = { ["errorCode"] = error.Code }
                    }),
                    Domain.Common.ErrorType.External => Results.Problem(
                        statusCode: StatusCodes.Status502BadGateway,
                        title: "External Service Error",
                        detail: error.Message,
                        extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code }),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error",
                        detail: error.Message)
                });
        }

        private static async Task<IResult> GetPayment(
            string id,
            ISender sender,
            CancellationToken cancellationToken)
        {
            var query = new GetPaymentQuery(id);
            var result = await sender.Send(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.Type switch
                {
                    Domain.Common.ErrorType.NotFound => Results.NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Payment Not Found",
                        Detail = error.Message,
                        Extensions = { ["errorCode"] = error.Code }
                    }),
                    Domain.Common.ErrorType.Validation => Results.BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Validation Error",
                        Detail = error.Message,
                        Extensions = { ["errorCode"] = error.Code }
                    }),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Internal Server Error",
                        detail: error.Message)
                });
        }
    }

    public record ProcessPaymentRequest(
        string CardNumber,
        int ExpiryMonth,
        int ExpiryYear,
        string Currency,
        int Amount,
        string Cvv);
}