
using PaymentGateway.Domain.Common;

namespace PaymentGateway.Domain.ValueObjects
{
    public sealed class CardNumber
    {
        public string Value { get; }
        public string LastFourDigits { get; }
        public string MaskedValue { get; }

        private CardNumber(string value)
        {
            Value = value;
            LastFourDigits = value[^4..];
            MaskedValue = new string('*', value.Length - 4) + LastFourDigits;
        }

        public static Result<CardNumber> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<CardNumber>.Failure(
                    Error.Validation("card_number.required", "Card number is required"));

            var cleanedValue = value.Replace(" ", "").Replace("-", "");

            if (!cleanedValue.All(char.IsDigit))
                return Result<CardNumber>.Failure(
                    Error.Validation("card_number.invalid_format", "Card number must contain only digits"));

            if (cleanedValue.Length < 14 || cleanedValue.Length > 19)
                return Result<CardNumber>.Failure(
                    Error.Validation("card_number.invalid_length", "Card number must be between 14 and 19 digits"));

            // Luhn algorithm validation for card numbers
            if (!IsValidLuhn(cleanedValue))
                return Result<CardNumber>.Failure(
                    Error.Validation("card_number.invalid_checksum", "Card number is invalid"));

            return Result<CardNumber>.Success(new CardNumber(cleanedValue));
        }

        private static bool IsValidLuhn(string cardNumber)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        public override string ToString() => MaskedValue;
    }
}
