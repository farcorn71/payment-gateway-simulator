using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PaymentGateway.Application.Common.Interfaces;
using PaymentGateway.Domain.Common;

using Polly;
using Polly.Retry;

namespace PaymentGateway.Infrastructure.BankClient
{
    public class AcquiringBankClient : IAcquiringBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AcquiringBankClient> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        public AcquiringBankClient(
            HttpClient httpClient,
            IOptions<BankClientOptions> options,
            ILogger<AcquiringBankClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _retryPolicy = Policy
                .HandleResult<HttpResponseMessage>(r =>
                    r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(
                    retryCount: options.Value.MaxRetries,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Bank service unavailable. Retry {RetryCount} after {Delay}ms",
                            retryCount, timespan.TotalMilliseconds);
                    });
        }

        public async Task<Result<BankAuthorizationResponse>> ProcessPaymentAsync(
            BankPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var bankRequest = new
                {
                    card_number = request.CardNumber,
                    expiry_date = request.ExpiryDate,
                    currency = request.Currency,
                    amount = request.Amount,
                    cvv = request.Cvv
                };

                _logger.LogInformation("Sending payment request to acquiring bank");

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.PostAsJsonAsync("/payments", bankRequest, cancellationToken));

                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    _logger.LogError("Bank service unavailable after all retries");
                    return Result<BankAuthorizationResponse>.Failure(
                        Error.External("bank.unavailable",
                            "The acquiring bank service is currently unavailable. Please try again later."));
                }

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Bank rejected request: {Error}", errorContent);
                    return Result<BankAuthorizationResponse>.Failure(
                        Error.External("bank.bad_request",
                            "The acquiring bank rejected the request due to invalid data."));
                }

                response.EnsureSuccessStatusCode();

                var bankResponse = await response.Content.ReadFromJsonAsync<BankResponse>(
                    cancellationToken: cancellationToken);

                if (bankResponse is null)
                {
                    _logger.LogError("Failed to deserialize bank response");
                    return Result<BankAuthorizationResponse>.Failure(
                        Error.External("bank.invalid_response",
                            "Received invalid response from acquiring bank."));
                }

                _logger.LogInformation("Received response from bank: Authorized={Authorized}",
                    bankResponse.Authorized);

                var result = new BankAuthorizationResponse(
                    bankResponse.Authorized,
                    bankResponse.AuthorizationCode);

                return Result<BankAuthorizationResponse>.Success(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error communicating with acquiring bank");
                return Result<BankAuthorizationResponse>.Failure(
                    Error.External("bank.connection_error",
                        "Failed to communicate with the acquiring bank."));
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request to acquiring bank timed out");
                return Result<BankAuthorizationResponse>.Failure(
                    Error.External("bank.timeout",
                        "Request to acquiring bank timed out."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error communicating with acquiring bank");
                return Result<BankAuthorizationResponse>.Failure(
                    Error.External("bank.unexpected_error",
                        "An unexpected error occurred while communicating with the acquiring bank."));
            }
        }

        private record BankResponse(
            bool Authorized,
            string? AuthorizationCode);
    }

}
