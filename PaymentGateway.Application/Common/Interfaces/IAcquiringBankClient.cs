using PaymentGateway.Domain.Common;

namespace PaymentGateway.Application.Common.Interfaces
{
    public interface IAcquiringBankClient
    {
        Task<Result<BankAuthorizationResponse>> ProcessPaymentAsync(
            BankPaymentRequest request,
            CancellationToken cancellationToken = default);
    }

    public record BankPaymentRequest(
        string CardNumber,
        string ExpiryDate,
        string Currency,
        int Amount,
        string Cvv);

    public record BankAuthorizationResponse(
        bool Authorized,
        string? AuthorizationCode);
}
