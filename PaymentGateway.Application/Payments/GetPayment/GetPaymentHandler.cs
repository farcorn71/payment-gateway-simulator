using MediatR;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Payments.GetPayment
{
    public sealed class GetPaymentHandler : IRequestHandler<GetPaymentQuery, Result<GetPaymentResponse>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<GetPaymentHandler> _logger;

        public GetPaymentHandler(
            IPaymentRepository paymentRepository,
            ILogger<GetPaymentHandler> logger)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<GetPaymentResponse>> Handle(
            GetPaymentQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Retrieving payment {PaymentId}", request.PaymentId);

            if (!PaymentId.TryParse(request.PaymentId, out var paymentId))
            {
                _logger.LogWarning("Invalid payment ID format: {PaymentId}", request.PaymentId);
                return Result<GetPaymentResponse>.Failure(
                    Error.Validation("payment_id.invalid", "Invalid payment ID format"));
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);

            if (payment is null)
            {
                _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                return Result<GetPaymentResponse>.Failure(
                    Error.NotFound("payment.not_found", $"Payment with ID '{paymentId}' was not found"));
            }

            _logger.LogInformation("Payment {PaymentId} retrieved successfully", paymentId);

            var response = new GetPaymentResponse(
                payment.Id.ToString(),
                payment.Status.ToString(),
                payment.CardNumber.LastFourDigits,
                payment.ExpiryDate.Month,
                payment.ExpiryDate.Year,
                payment.Money.Currency.Code,
                payment.Money.Amount);

            return Result<GetPaymentResponse>.Success(response);
        }
    }
}
