
using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.ValueObjects
{
    public class Cvv
    {
        public string Value { get; }

        private Cvv(string value)
        {
            Value = value;
        }

        public static Result<Cvv> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.required", "CVV is required"));

            var trimmedValue = value.Trim();

            if (!trimmedValue.All(char.IsDigit))
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.invalid_format", "CVV must contain only digits"));

            if (trimmedValue.Length < 3 || trimmedValue.Length > 4)
                return Result<Cvv>.Failure(
                    Error.Validation("cvv.invalid_length", "CVV must be 3 or 4 digits"));

            return Result<Cvv>.Success(new Cvv(trimmedValue));
        }

        public override string ToString() => "***";
    }
}
