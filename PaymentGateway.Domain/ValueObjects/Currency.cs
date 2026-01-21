
using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.ValueObjects
{
    public sealed class Currency
    {
        private static readonly HashSet<string> SupportedCurrencies = new()
    {
        "USD", // US Dollar
        "GBP", // British Pound
        "EUR"  // Euro
    };

        public string Code { get; }

        private Currency(string code)
        {
            Code = code;
        }

        public static Result<Currency> Create(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result<Currency>.Failure(
                    Error.Validation("currency.required", "Currency is required"));

            var upperCode = code.Trim().ToUpperInvariant();

            if (upperCode.Length != 3)
                return Result<Currency>.Failure(
                    Error.Validation("currency.invalid_length", "Currency must be 3 characters"));

            if (!SupportedCurrencies.Contains(upperCode))
                return Result<Currency>.Failure(
                    Error.Validation("currency.not_supported",
                        $"Currency '{upperCode}' is not supported. Supported currencies: {string.Join(", ", SupportedCurrencies)}"));

            return Result<Currency>.Success(new Currency(upperCode));
        }

        public override string ToString() => Code;
    }
}
