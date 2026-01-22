using MediatR;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.ValueObjects;

namespace PaymentGateway.Application.Payments.ProcessPayment
{
    public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, Result<ProcessPaymentResponse>>
    {
        private readonly IAcquiringBankClient _bankClient;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<ProcessPaymentHandler> _logger;

        public ProcessPaymentHandler(
            IAcquiringBankClient bankClient,
            IPaymentRepository paymentRepository,
            ILogger<ProcessPaymentHandler> logger)
        {
            _bankClient = bankClient;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        public async Task<Result<ProcessPaymentResponse>> Handle(
            ProcessPaymentCommand request,
            CancellationToken cancellationToken)
        {
            var paymentId = PaymentId.New();

            _logger.LogInformation("Processing payment {PaymentId}", paymentId);

            var cardNumberResult = CardNumber.Create(request.CardNumber);
            if (!cardNumberResult.IsSuccess)
            {
                _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, cardNumberResult.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(cardNumberResult.Error!);
            }

            var expiryDateResult = ExpiryDate.Create(request.ExpiryMonth, request.ExpiryYear);
            if (!expiryDateResult.IsSuccess)
            {
                _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, expiryDateResult.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(expiryDateResult.Error!);
            }

            var currencyResult = Currency.Create(request.Currency);
            if (!currencyResult.IsSuccess)
            {
                _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, currencyResult.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(currencyResult.Error!);
            }

            var moneyResult = Money.Create(request.Amount, currencyResult.Value!);
            if (!moneyResult.IsSuccess)
            {
                _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, moneyResult.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(moneyResult.Error!);
            }

            var cvvResult = Cvv.Create(request.Cvv);
            if (!cvvResult.IsSuccess)
            {
                _logger.LogWarning("Payment {PaymentId} rejected: {Error}", paymentId, cvvResult.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(cvvResult.Error!);
            }

            
            var bankRequest = new BankPaymentRequest(
                cardNumberResult.Value!.Value,
                expiryDateResult.Value!.ToFormattedString(),
                currencyResult.Value!.Code,
                moneyResult.Value!.Amount,
                cvvResult.Value!.Value);

            _logger.LogInformation("Sending payment {PaymentId} to acquiring bank", paymentId);

            var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest, cancellationToken);

            if (!bankResponse.IsSuccess)
            {
                _logger.LogError("Bank communication failed for payment {PaymentId}: {Error}",
                    paymentId, bankResponse.Error!.Message);
                return Result<ProcessPaymentResponse>.Failure(bankResponse.Error!);
            }

            
            var payment = bankResponse.Value!.Authorized
                ? Payment.CreateAuthorized(
                    paymentId,
                    cardNumberResult.Value!,
                    expiryDateResult.Value!,
                    moneyResult.Value!,
                    bankResponse.Value.AuthorizationCode!)
                : Payment.CreateDeclined(
                    paymentId,
                    cardNumberResult.Value!,
                    expiryDateResult.Value!,
                    moneyResult.Value!);

           
            await _paymentRepository.AddAsync(payment, cancellationToken);

            _logger.LogInformation("Payment {PaymentId} processed successfully with status {Status}",
                paymentId, payment.Status);

            
            var response = new ProcessPaymentResponse(
                paymentId.ToString(),
                payment.Status.ToString(),
                payment.CardNumber.LastFourDigits,
                payment.ExpiryDate.Month,
                payment.ExpiryDate.Year,
                payment.Money.Currency.Code,
                payment.Money.Amount,
                payment.AuthorizationCode);

            return Result<ProcessPaymentResponse>.Success(response);
        }
    }
}
