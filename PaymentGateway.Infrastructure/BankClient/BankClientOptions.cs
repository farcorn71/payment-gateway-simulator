
namespace PaymentGateway.Infrastructure.BankClient
{
    public sealed class BankClientOptions
    {
        public const string SectionName = "BankClient";

        public string BaseUrl { get; set; } = "http://localhost:8080";
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
    }
}
