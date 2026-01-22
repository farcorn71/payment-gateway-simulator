using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using PaymentGateway.Infrastructure.BankClient;

namespace PaymentGateway.Api
{
    public class BankHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BankClientOptions _options;

        public BankHealthCheck(
            IHttpClientFactory httpClientFactory,
            IOptions<BankClientOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync(_options.BaseUrl, cancellationToken);

                return response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? HealthCheckResult.Healthy("Acquiring bank is responsive")
                    : HealthCheckResult.Degraded($"Acquiring bank returned status code: {response.StatusCode}");
            }
            catch (HttpRequestException ex)
            {
                return HealthCheckResult.Unhealthy("Acquiring bank is not reachable", ex);
            }
            catch (TaskCanceledException ex)
            {
                return HealthCheckResult.Unhealthy("Acquiring bank health check timed out", ex);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Unexpected error checking bank health", ex);
            }
        }
    }

}
