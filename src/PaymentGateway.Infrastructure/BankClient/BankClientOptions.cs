
namespace PaymentGateway.Infrastructure.BankClient
{
    public class BankClientOptions
    {
        public const string SectionName = "BankClient";

        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public int MaxRetries { get; set; }
    }
}
