using MediatR;

using PaymentGateway.Domain.Common;

namespace PaymentGateway.Application.Payments.GetPayment
{
    public record GetPaymentQuery(string PaymentId) : IRequest<Result<GetPaymentResponse>>;

    public record GetPaymentResponse(
        string Id,
        string Status,
        string LastFourCardDigits,
        int ExpiryMonth,
        int ExpiryYear,
        string Currency,
        int Amount);
}
