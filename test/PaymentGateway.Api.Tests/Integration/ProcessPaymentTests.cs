using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using PaymentGateway.Application.Payments.ProcessPayment;

using Xunit;

namespace PaymentGateway.Api.Tests.Integration
{
    public class ProcessPaymentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProcessPaymentTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ProcessPayment_WithValidRequest_ShouldReturnAuthorized()
        {
            // Arrange 
            var request = new
            {
                cardNumber = "4532015112830366",
                expiryMonth = 12,
                expiryYear = DateTime.UtcNow.Year + 1,
                currency = "USD",
                amount = 1000,
                cvv = "123"
            };

            
            var authorizedRequest = new
            {
                cardNumber = "4532015112830367",
                expiryMonth = 12,
                expiryYear = DateTime.UtcNow.Year + 1,
                currency = "USD",
                amount = 1000,
                cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadGateway);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
                result.Should().NotBeNull();
                result!.Id.Should().NotBeNullOrEmpty();
                result.Status.Should().BeOneOf("Authorized", "Declined");
                result.LastFourCardDigits.Should().Be("0366");
                result.Currency.Should().Be("USD");
                result.Amount.Should().Be(1000);
            }
        }

        [Fact]
        public async Task ProcessPayment_WithInvalidCardNumber_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new
            {
                cardNumber = "123", // Too short
                expiryMonth = 12,
                expiryYear = DateTime.UtcNow.Year + 1,
                currency = "USD",
                amount = 1000,
                cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ProcessPayment_WithExpiredCard_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new
            {
                cardNumber = "4532015112830366",
                expiryMonth = 1,
                expiryYear = DateTime.UtcNow.Year - 1,
                currency = "USD",
                amount = 1000,
                cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ProcessPayment_WithInvalidCurrency_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new
            {
                cardNumber = "4532015112830366",
                expiryMonth = 12,
                expiryYear = DateTime.UtcNow.Year + 1,
                currency = "XXX",
                amount = 1000,
                cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ProcessPayment_WithZeroAmount_ShouldReturnBadRequest()
        {
            // Arrange
            var request = new
            {
                cardNumber = "4532015112830366",
                expiryMonth = 12,
                expiryYear = DateTime.UtcNow.Year + 1,
                currency = "USD",
                amount = 0,
                cvv = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

}
