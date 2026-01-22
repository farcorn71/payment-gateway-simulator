using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Application.Payments.ProcessPayment;
using PaymentGateway.Domain.Common;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

using Xunit;

namespace PaymentGateway.Application.Tests.Payments
{
    public class ProcessPaymentHandlerTests
    {
        private readonly IAcquiringBankClient _bankClient;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<ProcessPaymentHandler> _logger;
        private readonly ProcessPaymentHandler _handler;

        public ProcessPaymentHandlerTests()
        {
            _bankClient = Substitute.For<IAcquiringBankClient>();
            _paymentRepository = Substitute.For<IPaymentRepository>();
            _logger = Substitute.For<ILogger<ProcessPaymentHandler>>();
            _handler = new ProcessPaymentHandler(_bankClient, _paymentRepository, _logger);
        }

        [Fact]
        public async Task Handle_WithValidRequestAndAuthorizedBank_ShouldReturnAuthorizedPayment()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "USD",
                Amount: 1000,
                Cvv: "123");

            var bankResponse = new BankAuthorizationResponse(
                Authorized: true,
                AuthorizationCode: "auth-123");

            _bankClient.ProcessPaymentAsync(
                Arg.Any<BankPaymentRequest>(),
                Arg.Any<CancellationToken>())
                .Returns(Result<BankAuthorizationResponse>.Success(bankResponse));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Status.Should().Be("Authorized");
            result.Value.AuthorizationCode.Should().Be("auth-123");
            result.Value.LastFourCardDigits.Should().Be("8877");
            result.Value.Currency.Should().Be("USD");
            result.Value.Amount.Should().Be(1000);

            await _paymentRepository.Received(1).AddAsync(
                Arg.Is<Payment>(p => p.Status == PaymentStatus.Authorized),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithValidRequestAndDeclinedBank_ShouldReturnDeclinedPayment()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "4532015112830366",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "GBP",
                Amount: 500,
                Cvv: "456");

            var bankResponse = new BankAuthorizationResponse(
                Authorized: false,
                AuthorizationCode: null);

            _bankClient.ProcessPaymentAsync(
                Arg.Any<BankPaymentRequest>(),
                Arg.Any<CancellationToken>())
                .Returns(Result<BankAuthorizationResponse>.Success(bankResponse));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Status.Should().Be("Declined");
            result.Value.AuthorizationCode.Should().BeNull();

            await _paymentRepository.Received(1).AddAsync(
                Arg.Is<Payment>(p => p.Status == PaymentStatus.Declined),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithInvalidCardNumber_ShouldReturnValidationError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "123",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "USD",
                Amount: 1000,
                Cvv: "123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Contain("card_number");

            await _bankClient.DidNotReceive().ProcessPaymentAsync(
                Arg.Any<BankPaymentRequest>(),
                Arg.Any<CancellationToken>());

            await _paymentRepository.DidNotReceive().AddAsync(
                Arg.Any<Payment>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithExpiredCard_ShouldReturnValidationError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 1,
                ExpiryYear: 2020,
                Currency: "USD",
                Amount: 1000,
                Cvv: "123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Contain("expiry");

            await _bankClient.DidNotReceive().ProcessPaymentAsync(
                Arg.Any<BankPaymentRequest>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithInvalidCurrency_ShouldReturnValidationError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "XXX",
                Amount: 1000,
                Cvv: "123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Contain("currency");
        }

        [Fact]
        public async Task Handle_WithZeroAmount_ShouldReturnValidationError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "USD",
                Amount: 0, 
                Cvv: "123");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Contain("amount");
        }

        [Fact]
        public async Task Handle_WhenBankFails_ShouldReturnBankError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "EUR",
                Amount: 2000,
                Cvv: "789");

            _bankClient.ProcessPaymentAsync(
                Arg.Any<BankPaymentRequest>(),
                Arg.Any<CancellationToken>())
                .Returns(Result<BankAuthorizationResponse>.Failure(
                    Error.External("bank.unavailable", "Bank service unavailable")));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.External);
            result.Error.Code.Should().Be("bank.unavailable");

            await _paymentRepository.DidNotReceive().AddAsync(
                Arg.Any<Payment>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_WithInvalidCvv_ShouldReturnValidationError()
        {
            // Arrange
            var command = new ProcessPaymentCommand(
                CardNumber: "2222405343248877",
                ExpiryMonth: 12,
                ExpiryYear: DateTime.UtcNow.Year + 1,
                Currency: "USD",
                Amount: 1000,
                Cvv: "12");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error!.Type.Should().Be(ErrorType.Validation);
            result.Error.Code.Should().Contain("cvv");
        }
    }

}
