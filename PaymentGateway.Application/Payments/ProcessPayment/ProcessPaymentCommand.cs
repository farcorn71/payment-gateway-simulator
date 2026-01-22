using MediatR;

using PaymentGateway.Domain.Common;

namespace PaymentGateway.Application.Payments.ProcessPayment
{
    public record ProcessPaymentCommand(
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Currency,
    int Amount,
    string Cvv) : IRequest<Result<ProcessPaymentResponse>>;

    public record ProcessPaymentResponse(
        string Id,
        string Status,
        string LastFourCardDigits,
        int ExpiryMonth,
        int ExpiryYear,
        string Currency,
        int Amount,
        string? AuthorizationCode = null);

}
