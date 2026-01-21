

using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.ValueObjects
{
    public sealed class Money
    {
        public int Amount { get; } // Amount in minor units (cents, pence, etc.)
        public Currency Currency { get; }

        private Money(int amount, Currency currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Result<Money> Create(int amount, Currency currency)
        {
            if (amount <= 0)
                return Result<Money>.Failure(
                    Error.Validation("amount.invalid", "Amount must be greater than zero"));

            if (amount > 999999999) // Max ~$10M to prevent overflow
                return Result<Money>.Failure(
                    Error.Validation("amount.too_large", "Amount exceeds maximum allowed value"));

            return Result<Money>.Success(new Money(amount, currency));
        }

        public decimal ToMajorUnits() => Amount / 100m;

        public override string ToString() => $"{Currency.Code} {ToMajorUnits():F2}";
    }
}
