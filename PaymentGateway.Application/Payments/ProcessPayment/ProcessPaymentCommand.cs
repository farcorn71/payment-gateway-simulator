using MediatR;

using PaymentGateway.Domain.Common;

namespace PaymentGateway.Application.Payments.ProcessPayment
{
    public sealed record ProcessPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv) : IRequest<Result<ProcessPaymentResponse>>;

    public sealed record ProcessPaymentResponse(
        string Id,
        string Status,
        string LastFourCardDigits,
        int ExpiryMonth,
        int ExpiryYear,
        string Currency,
        int Amount,
        string? AuthorizationCode = null);

}
